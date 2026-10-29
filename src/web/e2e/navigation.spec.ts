import { expect, test } from '@playwright/test';

test('the header shows both services up', async ({ page }) => {
  await page.goto('/');

  const health = page.getByRole('list', { name: 'Service health' });
  await expect(health.getByRole('listitem')).toHaveText(['Archive is up', 'Export is up']);
});

test('the tabs open each page and the name button goes home', async ({ page }) => {
  await page.goto('/');
  const nav = page.getByRole('navigation', { name: 'Pages' });

  await nav.getByRole('link', { name: 'Upload' }).click();
  await expect(page).toHaveURL(/#\/upload$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Upload a folder');

  await nav.getByRole('link', { name: 'Export' }).click();
  await expect(page).toHaveURL(/#\/export$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Export documents');

  await page.getByRole('link', { name: 'Document Archive' }).click();
  await expect(page).toHaveURL(/#\/cabinets$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('File cabinets');
});
