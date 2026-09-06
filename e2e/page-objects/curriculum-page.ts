import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class CurriculumPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/curriculum', { waitUntil: 'domcontentloaded' }); }
  async expectPath() { await expect(this.page.getByRole('list', { name: 'Module path' }).getByRole('listitem')).toHaveCount(12); }
  async resume() { await this.page.getByRole('link', { name: 'Continue module' }).click(); }
  async expectReady() { await this.expectPath(); await expect(this.page.getByRole('link', { name: 'Continue module' })).toBeVisible(); }
  async expectProgress(completed: number) { await expect(this.page.getByText(`${completed} of 12 complete`, { exact: true })).toBeVisible(); }
  async expectCurrent(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
}

