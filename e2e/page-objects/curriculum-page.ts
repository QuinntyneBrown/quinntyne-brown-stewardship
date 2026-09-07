import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class CurriculumPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/curriculum', { waitUntil: 'domcontentloaded' }); }
  async expectPath(count = 12) { await expect(this.page.getByRole('list', { name: 'Module path' }).getByRole('listitem')).toHaveCount(count); }
  async resume() { await this.page.getByRole('link', { name: 'Continue module' }).click(); }
  async expectReady(count = 12) { await this.expectPath(count); await expect(this.page.getByRole('link', { name: 'Continue module' })).toBeVisible(); }
  async expectProgress(completed: number, total = 12) { await expect(this.page.getByText(`${completed} of ${total} complete`, { exact: true })).toBeVisible(); }
  async expectRemaining(remaining: number) { await expect(this.page.getByText(new RegExp(`^${remaining} module${remaining === 1 ? ' remains' : 's remain'} ·`))).toBeVisible(); }
  async expectWeek(week: number, duration: number) { await expect(this.page.getByText(`Week ${week} of ${duration}`)).toBeVisible(); }
  async expectCurrent(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
  async expectNotYetAvailable() {
    await expect(this.page.getByRole('heading', { name: 'Your programme is not yet available.' })).toBeVisible();
    await expect(this.page.getByRole('list', { name: 'Module path' })).toHaveCount(0);
    await expect(this.page.getByRole('main')).not.toContainText(/\d+ of \d+ complete|modules? remains?/);
  }
  async expectNotice(text: string) { await expect(this.page.getByRole('status').filter({ hasText: text })).toBeVisible(); }
}
