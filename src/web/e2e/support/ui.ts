import type { Download, Page } from '@playwright/test';
import { fixture } from './stack';

export const responsePanel = (page: Page) => page.getByRole('region', { name: 'Response' });
export const responseBadge = (page: Page) => responsePanel(page).locator('.badge');
export const submitButton = (page: Page) => page.locator('form button[type=submit]');
export const toast = (page: Page) => page.getByRole('alert');

export async function startUpload(page: Page, cabinet: string, field: string, zip: string) {
  await page.goto('/#/upload');
  await page.getByRole('combobox', { name: 'Cabinet', exact: true }).selectOption(cabinet);
  await page.getByRole('combobox', { name: 'Field for the folder name' }).selectOption(field);
  await page.getByLabel('Folder as a zip').setInputFiles(fixture(zip));
  await submitButton(page).click();
}

export async function startExport(page: Page, cabinet: string, field: string, from: string, to: string) {
  await page.goto('/#/export');
  await page.getByRole('combobox', { name: 'Cabinet', exact: true }).selectOption(cabinet);
  await page.getByRole('combobox', { name: 'Field for the export marker' }).selectOption(field);
  await page.getByLabel('From', { exact: true }).fill(from);
  await page.getByLabel('To', { exact: true }).fill(to);
  await submitButton(page).click();
}

export async function exportAndDownload(page: Page, cabinet: string, field: string, from: string, to: string): Promise<Download> {
  const download = page.waitForEvent('download');
  await startExport(page, cabinet, field, from, to);
  return download;
}

export function countRequests(page: Page, method: string, pathEnd: string): () => number {
  let count = 0;
  page.on('request', request => {
    if (request.method() === method && new URL(request.url()).pathname.endsWith(pathEnd)) {
      count++;
    }
  });
  return () => count;
}

export async function delayRequests(page: Page, pattern: string, delayMs: number) {
  await page.route(pattern, async route => {
    await new Promise(resolve => setTimeout(resolve, delayMs));
    await route.continue();
  });
}
