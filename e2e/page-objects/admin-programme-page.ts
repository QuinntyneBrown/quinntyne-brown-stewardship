import { AdminScreenPage } from './admin-screen-page';
import { expect, Page } from '@playwright/test';
export class AdminProgrammePage extends AdminScreenPage {
  constructor(page: Page) { super(page); }
  async open(id: string) { await this.page.goto(`/admin/programmes/${id}`); }
  async expectHeading(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
  async expectMeta(text: string) { await expect(this.page.getByText(text)).toBeVisible(); }
  async renameTo(title: string) { await this.page.getByLabel('Title', { exact: true }).fill(title); await this.page.getByRole('button', { name: 'Save title', exact: true }).click(); }
  async typeTitle(title: string) { await this.page.getByLabel('Title', { exact: true }).fill(title); }
  async rekeyTo(key: string) { await this.page.getByLabel('Key', { exact: true }).fill(key); await this.page.getByRole('button', { name: 'Save key', exact: true }).click(); }
  async expectKeyLocked() {
    await expect(this.page.getByText('The key cannot be changed while cohorts follow it.')).toBeVisible();
    await expect(this.page.getByLabel('Key', { exact: true })).toHaveCount(0);
  }
  async expectKeyError(text: string) { await expect(this.page.getByLabel('Key', { exact: true })).toHaveAttribute('aria-invalid', 'true'); await this.expectAlert(text); }
  async expectModules(count: number) { await expect(this.page.getByRole('list', { name: 'Modules' }).getByRole('listitem')).toHaveCount(count); }
  async expectNoModules() { await expect(this.page.getByRole('heading', { name: 'No modules yet' })).toBeVisible(); }
  async expectRemovalBlocked(reason: string) {
    await expect(this.page.getByRole('button', { name: 'Remove programme', exact: true })).toBeDisabled();
    await expect(this.page.getByText(reason)).toBeVisible();
  }
  // Removal asks first. Focus enters the dialog, stays inside it while it is open, and returns to the control when it closes.
  async removeAfterConfirming() {
    const trigger = this.page.getByRole('button', { name: 'Remove programme', exact: true });
    await trigger.click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove this programme?' })).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Keep programme' })).toBeFocused();
    await this.page.keyboard.press('Tab'); await expect(dialog.getByRole('button', { name: 'Remove programme' })).toBeFocused();
    await this.page.keyboard.press('Shift+Tab'); await expect(dialog.getByRole('button', { name: 'Keep programme' })).toBeFocused();
    await this.page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused();
    await trigger.click(); await dialog.getByRole('button', { name: 'Remove programme' }).click();
  }
  async leaveKeepingChanges() {
    await this.page.getByRole('navigation', { name: 'Authoring trail' }).getByRole('link', { name: 'Programmes' }).first().click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Leave without saving?' })).toBeVisible();
    await dialog.getByRole('button', { name: 'Keep editing' }).click();
    await expect(dialog).not.toBeVisible();
  }
  async publish() { await this.page.getByRole("button", { name: /Publish (programme|again)/ }).click(); }
  async expectPublicationRefused(reason: string) { await this.expectAlert(`Publication refused. ${reason} Nothing was published, and what participants read is unchanged.`); }
  async expectPublication(state: "Draft" | "Published", detail: string) {
    const panel = this.page.getByRole("region", { name: "Publication" });
    await expect(panel.getByText(`Publication state: ${state}`)).toBeVisible(); await expect(panel.getByText(detail)).toBeVisible();
  }
  async expectReadiness(text: string) { await expect(this.page.getByRole("region", { name: "Publication" }).getByText(text)).toBeVisible(); }
  async expectShape(publishedModules: number, sections: number, prompts: number) {
    const shape = this.page.getByRole("region", { name: "Shape" });
    await expect(shape.getByText(`${publishedModules} markers`)).toBeVisible();
    for (const [term, value] of [["Published modules", publishedModules], ["Sections", sections], ["Preparation prompts", prompts]] as const) await expect(shape.getByRole("definition").filter({ has: this.page.locator(`xpath=preceding-sibling::dt[text()="${term}"]`) })).toHaveText(String(value));
  }
  async expectCohortsFollowing(...rows: string[]) {
    const list = this.page.getByRole("list", { name: "Cohorts following" });
    await expect(list.getByRole("listitem")).toHaveCount(rows.length);
    for (const [index, row] of rows.entries()) await expect(list.getByRole("listitem").nth(index)).toContainText(row);
  }
  async addModule(title: string, summary: string) {
    await this.page.getByLabel('Module title').fill(title); await this.page.getByLabel('Module summary').fill(summary);
    await this.page.getByRole('button', { name: 'Add module', exact: true }).click();
  }
  async moveModuleUp(title: string, position: number, total: number) { await this.page.getByRole('button', { name: `Move ${title} up from position ${position} of ${total}`, exact: true }).click(); }
  async moveModuleUpByKeyboard(title: string, position: number, total: number) { await this.activateByKeyboard(this.page.getByRole('button', { name: `Move ${title} up from position ${position} of ${total}`, exact: true })); }
  async expectModuleOrder(...titles: string[]) {
    const rows = this.page.getByRole('list', { name: 'Modules' }).getByRole('listitem');
    for (const [index, title] of titles.entries()) await expect(rows.nth(index)).toContainText(title);
  }
  async expectModuleCannotMove(title: string, position: number, total: number, direction: 'up' | 'down') {
    await expect(this.page.getByRole('button', { name: direction === 'up' ? `${title} is first of ${total} and cannot move up` : `${title} is last of ${total} and cannot move down`, exact: true })).toBeDisabled();
  }
  async edit(title: string) { await this.page.getByRole('link', { name: `Edit ${title}`, exact: true }).click(); }
  async expectTitleValue(title: string) { await expect(this.page.getByLabel('Title', { exact: true })).toHaveValue(title); }
}
