import { AdminScreenPage } from './admin-screen-page';
import { expect, Page } from '@playwright/test';
export class AdminSectionPage extends AdminScreenPage {
  constructor(page: Page) { super(page); }
  async open(id: string) { await this.page.goto(`/admin/sections/${id}`); }
  async expectHeading(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
  async expectPosition(ordinal: number, total: number) { await expect(this.page.getByText(`Section ${ordinal} of ${total}`, { exact: true })).toBeVisible(); }
  async fill(label: string, value: string) { await this.page.getByLabel(label, { exact: true }).fill(value); }
  async expectValue(label: string, value: string) { await expect(this.page.getByLabel(label, { exact: true })).toHaveValue(value); }
  async expectCount(text: string) { await expect(this.page.getByText(text, { exact: true })).toBeVisible(); }
  async expectReadingOver() { await expect(this.page.getByLabel('Reading content', { exact: true })).toHaveAttribute('aria-invalid', 'true'); }
  async save() { await this.page.getByRole('button', { name: 'Save section', exact: true }).click(); }
  async expectSaved() { await this.expectStatus('Section saved.'); }
  async expectRefused(count: number, text: string) {
    await this.expectStatus(`${count} field${count === 1 ? ' was' : 's were'} refused. Nothing was stored.`);
    await expect(this.page.getByRole('alert').filter({ hasText: text })).toBeVisible();
  }
  async expectRecorded(text: string) { await expect(this.page.getByRole('region', { name: 'Recorded against this section' }).getByText(text)).toBeVisible(); }
  async expectRemovalBlocked() {
    await expect(this.page.getByRole('button', { name: 'Remove section', exact: true })).toBeDisabled();
    await expect(this.page.getByText('This section cannot be removed while those records stand.')).toBeVisible();
  }
  async removeAfterConfirming() {
    const trigger = this.page.getByRole('button', { name: 'Remove section', exact: true });
    await trigger.click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove this section?' })).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Keep it' })).toBeFocused();
    await this.page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused();
    await trigger.click(); await dialog.getByRole('button', { name: 'Remove section' }).click();
  }
  async expectSiblings(...titles: string[]) {
    const list = this.page.getByRole('list', { name: 'Sections of this module' });
    await expect(list.getByRole('listitem')).toHaveCount(titles.length);
    for (const [index, title] of titles.entries()) await expect(list.getByRole('listitem').nth(index)).toContainText(title);
  }
}
