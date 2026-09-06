import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class NoteEditorPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async write(body: string) { await this.page.getByRole('textbox', { name: 'Your note' }).fill(body); }
  async save() { await this.page.getByRole('button', { name: 'Save note', exact: true }).click(); }
  async expectBody(body: string) { await expect(this.page.getByRole('textbox', { name: 'Your note' })).toHaveValue(body); }
  async keepUnsavedDraft() {
    this.page.once('dialog', async dialog => { expect(dialog.message()).toContain('unsaved'); await dialog.dismiss(); });
    await this.page.getByRole('link', { name: 'Back to notes' }).click();
  }
}

