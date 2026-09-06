import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class ModulePage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async completeSection() { await this.page.getByRole('button', { name: 'Mark section complete' }).click(); }
  async expectSection(position: number) { await expect(this.page.getByText(`Section ${position} of 5`, { exact: true })).toBeVisible(); }
  async reload() { await this.page.reload(); }
  async addNote() { await this.page.getByRole('link', { name: 'Add a module note' }).click(); }
}

