import { AdminScreenPage } from './admin-screen-page';
import { expect, Page } from '@playwright/test';
export class AdminProgrammesPage extends AdminScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/admin/programmes'); }
  async expectHeading() { await expect(this.page.getByRole('heading', { level: 1, name: 'Programmes' })).toBeVisible(); }
  async expectProgramme(title: string, state: 'Draft' | 'Published') {
    const row = this.page.getByRole('list', { name: 'Programmes' }).getByRole('listitem').filter({ hasText: title });
    await expect(row).toBeVisible(); await expect(row).toContainText(state);
    await expect(row.getByRole('link', { name: `Open ${title} for authoring` })).toBeVisible();
  }
  async create(key: string, title: string) {
    await this.page.getByLabel("Key").fill(key); await this.page.getByLabel("Title").fill(title);
    await this.page.getByRole("button", { name: "Create programme", exact: true }).click();
  }
  async expectKeyRefused(text: string) {
    await this.expectAlert(text); await expect(this.page.getByLabel("Key")).toBeFocused(); await expect(this.page.getByLabel("Key")).toHaveAttribute("aria-invalid", "true");
  }
  async expectTypedTitle(title: string) { await expect(this.page.getByLabel("Title")).toHaveValue(title); }
  async expectProgrammeCount(count: number) { await expect(this.page.getByRole("list", { name: "Programmes" }).getByRole("listitem")).toHaveCount(count); }
  async expectAbsentProgramme(title: string) { await expect(this.page.getByRole("list", { name: "Programmes" }).getByRole("listitem").filter({ hasText: title })).toHaveCount(0); }
  async expectRowDetail(title: string, detail: string) { await expect(this.page.getByRole("list", { name: "Programmes" }).getByRole("listitem").filter({ hasText: title })).toContainText(detail); }
  async expectEmptyState() {
    await expect(this.page.getByRole('heading', { name: 'No programmes yet' })).toBeVisible();
    await expect(this.page.getByRole('list', { name: 'Programmes' })).toHaveCount(0);
  }
  async expectAbsent() {
    await expect(this.page.getByRole('heading', { level: 1, name: 'Programmes' })).toHaveCount(0);
    await expect(this.page.getByRole('list', { name: 'Programmes' })).toHaveCount(0);
  }
}
