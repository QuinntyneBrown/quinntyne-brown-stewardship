import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class SessionsPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/sessions'); }
  async selectFirstOpen() { await this.page.getByRole('button', { name: /Open ·/ }).first().click(); }
  async book() { await this.page.getByRole('button', { name: 'Confirm booking', exact: true }).click(); }
  async expectBooking() { await expect(this.page.getByRole('heading', { name: 'Your next session', exact: true })).toBeVisible(); }
  async expectConflict() { await expect(this.page.getByRole('alert')).toContainText('no longer available'); await expect(this.page.getByRole('button', { name: /Taken ·/ })).toBeDisabled(); }
  async reschedule() { await this.page.getByRole('button', { name: 'Reschedule', exact: true }).click(); await this.selectFirstOpen(); await this.page.getByRole('button', { name: 'Confirm change' }).click(); }
  async cancel() { await this.page.getByRole('button', { name: 'Cancel session', exact: true }).click(); await this.page.getByRole('dialog').getByRole('button', { name: 'Yes, cancel session' }).click(); }
  async expectCanBook() { await expect(this.page.getByRole('button', { name: 'Confirm booking', exact: true })).toBeVisible(); }
  async prepare() { await this.page.getByRole('link', { name: 'Prepare for your session' }).click(); }
}

