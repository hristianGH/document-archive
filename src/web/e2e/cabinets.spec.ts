import { expect, test } from '@playwright/test';
import { responsePanel } from './support/ui';

test('the File cabinet page lists the cabinets and their fields', async ({ page }) => {
  await page.goto('/');

  const cabinet = page.getByRole('combobox', { name: 'Cabinet', exact: true });
  await expect(cabinet.getByRole('option')).toHaveText(['Invoices', 'Contracts']);

  const rows = responsePanel(page).getByRole('row');
  await expect(rows).toHaveCount(1 + 7);
  await expect(rows.filter({ hasText: 'InvoiceNumber' }).getByRole('cell')).toHaveText(['Invoice number', 'InvoiceNumber', 'text', '20', 'Yes']);
  await expect(rows.filter({ hasText: 'ApprovedBy' }).getByRole('cell')).toHaveText(['Approved by', 'ApprovedBy', 'text', '60', 'No']);
  await expect(rows.filter({ hasText: 'Amount' }).getByRole('cell')).toHaveText(['Amount', 'Amount', 'number', '—', 'Yes']);

  await cabinet.selectOption('Contracts');
  await expect(rows).toHaveCount(1 + 6);
  await expect(rows.filter({ hasText: 'Category' }).getByRole('cell')).toHaveText(['Category', 'Category', 'text', '40', 'No']);
});
