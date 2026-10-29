import { expect, test } from '@playwright/test';
import { fieldValues } from './support/archive-api';
import { fixture, todayUtc } from './support/stack';
import { countRequests, delayRequests, exportAndDownload, responseBadge, responsePanel, startExport, startUpload, submitButton, toast } from './support/ui';
import { readZip } from './support/zips';

// Contracts is used only here, so every export below covers exactly the two uploads of the sample.
test.describe.configure({ mode: 'serial' });

const cabinet = 'Contracts';
const today = todayUtc();
const marker = `Exported on ${today}`;
const uploadedDocuments = 2 * 12;

const rootFiles = ['invoice-001.txt', 'invoice-002.txt', 'notes.md', 'report.csv', 'readme with spaces.txt', 'Договор София.txt', 'scan.pdf', 'photo.png', 'empty-note.txt'];
const folders: Record<string, string[]> = {
  'delivery-2026-03': ['page1.txt', 'page2.txt', 'page10.txt'],
  'Фактури Q1': ['фактура-1.txt', 'фактура-2.txt'],
  'empty-folder': []
};

// The sample uploaded twice: the second copy of every name gets ' (2)'. Files nested too deep were never stored.
async function expectedExport(): Promise<Record<string, string>> {
  const sample = await readZip(fixture('sample-material.zip'));
  const expected: Record<string, string> = {};
  for (const copy of ['', ' (2)']) {
    for (const file of rootFiles) {
      const dot = file.lastIndexOf('.');
      expected[`${file.slice(0, dot)}${copy}${file.slice(dot)}`] = sample[`sample-material/${file}`];
    }

    for (const [folder, parts] of Object.entries(folders)) {
      if (parts.length === 0) {
        expected[`${folder}${copy}/`] = '';
      }

      for (const part of parts) {
        expected[`${folder}${copy}/${part}`] = sample[`sample-material/${folder}/${part}`];
      }
    }
  }

  return expected;
}

test('seed: upload the sample twice to Contracts through the UI', async ({ page }) => {
  for (let upload = 0; upload < 2; upload++) {
    await startUpload(page, cabinet, 'Notes', 'sample-material.zip');
    await expect(responseBadge(page)).toHaveText('Success');
  }
});

test('the export zip holds every document, byte for byte, across several pages', async ({ page }) => {
  const download = await exportAndDownload(page, cabinet, 'ContractNumber', today, today);

  expect(download.suggestedFilename()).toBe(`Contracts-export-${today}.zip`);
  expect(await readZip(await download.path())).toEqual(await expectedExport());
  await expect(responseBadge(page)).toHaveText('Success');
  await expect(responsePanel(page)).toContainText(`Downloaded Contracts-export-${today}.zip.`);
});

test('every exported document is marked in the chosen field', async ({ request }) => {
  expect(await fieldValues(request, cabinet, today, 'ContractNumber')).toEqual(Array(uploadedDocuments).fill(marker));
});

test('a second export includes the marked documents again and overwrites the chosen field', async ({ page, request }) => {
  expect(await fieldValues(request, cabinet, today, 'Notes')).toEqual(Array(uploadedDocuments).fill('sample-material'));

  const download = await exportAndDownload(page, cabinet, 'Notes', today, today);

  expect(await readZip(await download.path())).toEqual(await expectedExport());
  expect(await fieldValues(request, cabinet, today, 'Notes')).toEqual(Array(uploadedDocuments).fill(marker));
});

test('a field too short for the marker is refused before anything is exported', async ({ page }) => {
  let downloads = 0;
  page.on('download', () => downloads++);

  await startExport(page, 'Invoices', 'InvoiceNumber', today, today);

  const reason = `The export marker '${marker}' is 22 characters long, but 'Invoice number' (InvoiceNumber) holds at most 20.`;
  await expect(responseBadge(page)).toHaveText('Error 400');
  await expect(responsePanel(page)).toContainText(reason);
  await expect(toast(page)).toContainText('Export failed');
  await expect(toast(page)).toContainText(reason);
  expect(downloads).toBe(0);
});

test('a date range without documents says so', async ({ page }) => {
  await startExport(page, cabinet, 'ContractNumber', '2000-01-01', '2000-01-31');

  await expect(responseBadge(page)).toHaveText('Error 404');
  await expect(responsePanel(page)).toContainText('No documents in Contracts in this range.');
  await expect(toast(page)).toContainText('No documents in Contracts in this range.');
});

test('an export cannot be started twice while it runs', async ({ page }) => {
  const exports = countRequests(page, 'POST', '/exports');
  await delayRequests(page, '**/api/export/exports', 1500);

  const download = page.waitForEvent('download');
  await startExport(page, cabinet, 'ContractNumber', today, today);
  await expect(submitButton(page)).toBeDisabled();
  await expect(submitButton(page)).toHaveText('Exporting…');
  await expect(responseBadge(page)).toHaveText('Sending…');
  await submitButton(page).click({ force: true });

  await download;
  await expect(responseBadge(page)).toHaveText('Success');
  expect(exports()).toBe(1);
});
