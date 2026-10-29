import { expect, test, type Page } from '@playwright/test';
import { countRequests, delayRequests, responseBadge, responsePanel, startUpload, submitButton, toast } from './support/ui';

const reportSection = (page: Page, title: string) =>
  responsePanel(page).locator('.report-section').filter({ has: page.getByRole('heading', { name: title }) });

async function expectStored(page: Page, documents: [title: string, parts: string][]) {
  const items = reportSection(page, 'Stored').getByRole('listitem');
  await expect(items).toHaveCount(documents.length);
  for (const [title, parts] of documents) {
    await expect(items.filter({ hasText: title })).toHaveText(`${title} · ${parts}`);
  }
}

test('uploading the sample stores 12 documents and skips what is nested too deep', async ({ page }) => {
  await startUpload(page, 'Invoices', 'Batch', 'sample-material.zip');

  await expect(responseBadge(page)).toHaveText('Success');
  await expect(responsePanel(page)).toContainText('Every stored document has Batch set to sample-material.');
  await expectStored(page, [
    ['invoice-001.txt', '1 part'],
    ['invoice-002.txt', '1 part'],
    ['notes.md', '1 part'],
    ['report.csv', '1 part'],
    ['readme with spaces.txt', '1 part'],
    ['Договор София.txt', '1 part'],
    ['scan.pdf', '1 part'],
    ['photo.png', '1 part'],
    ['empty-note.txt', '1 part'],
    ['delivery-2026-03', '3 parts'],
    ['Фактури Q1', '2 parts'],
    ['empty-folder', '0 parts']
  ]);
  await expect(reportSection(page, 'Skipped').getByRole('listitem')).toHaveText([
    'sample-material/delivery-2026-03/archive/ · Nested deeper than one level.',
    'sample-material/delivery-2026-03/archive/old-page.txt · Nested deeper than one level.'
  ]);
  await expect(reportSection(page, 'Failed')).toContainText('None');
});

test('a short folder name fits the Status field', async ({ page }) => {
  await startUpload(page, 'Invoices', 'Status', 'delivery-04.zip');

  await expect(responseBadge(page)).toHaveText('Success');
  await expect(responsePanel(page)).toContainText('Every stored document has Status set to delivery-04.');
  await expectStored(page, [
    ['cover.txt', '1 part'],
    ['pages', '3 parts'],
    ['blank', '0 parts']
  ]);
  await expect(reportSection(page, 'Skipped').getByRole('listitem')).toHaveText([
    'delivery-04/pages/old/ · Nested deeper than one level.',
    'delivery-04/pages/old/old-page.txt · Nested deeper than one level.'
  ]);
});

test('a folder name too long for the field shows the reason in the panel and a toast', async ({ page }) => {
  await startUpload(page, 'Invoices', 'Status', 'sample-material.zip');

  const reason = "Value 'sample-material' is longer than the 12 characters allowed for field 'Status'.";
  await expect(responseBadge(page)).toHaveText('Error 400');
  await expect(responsePanel(page)).toContainText(reason);
  await expect(toast(page)).toContainText('Upload failed');
  await expect(toast(page)).toContainText(reason);
});

test('a file that is not a zip shows the reason in the panel and a toast', async ({ page }) => {
  await startUpload(page, 'Invoices', 'Batch', 'not-a-zip.zip');

  await expect(responseBadge(page)).toHaveText('Error 400');
  await expect(responsePanel(page)).toContainText('The file is not a valid zip.');
  await expect(toast(page)).toContainText('The file is not a valid zip.');
});

test('an upload cannot be started twice while it runs', async ({ page }) => {
  const uploads = countRequests(page, 'POST', '/uploads');
  await delayRequests(page, '**/api/archive/cabinets/*/uploads', 1500);

  await startUpload(page, 'Invoices', 'Batch', 'delivery-04.zip');
  await expect(submitButton(page)).toBeDisabled();
  await expect(submitButton(page)).toHaveText('Uploading…');
  await expect(responseBadge(page)).toHaveText('Sending…');
  await submitButton(page).click({ force: true });

  await expect(responseBadge(page)).toHaveText('Success');
  expect(uploads()).toBe(1);
});
