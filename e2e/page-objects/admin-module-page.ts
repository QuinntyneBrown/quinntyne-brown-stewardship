import { AdminScreenPage } from './admin-screen-page';
import { expect, Page } from '@playwright/test';
export class AdminModulePage extends AdminScreenPage {
  constructor(page: Page) { super(page); }
  async open(id: string) { await this.page.goto(`/admin/modules/${id}`); }
  async expectHeading(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
  async expectPosition(ordinal: number, total: number) {
    await expect(this.page.getByText(`Module ${String(ordinal).padStart(2, '0')} of ${total}`)).toBeVisible();
    await expect(this.page.getByRole('definition').filter({ has: this.page.locator('xpath=preceding-sibling::dt[text()="Read in week"]') })).toHaveText(String(ordinal));
  }
  async fill(label: string, value: string) { await this.page.getByLabel(label, { exact: true }).fill(value); }
  async expectValue(label: string, value: string) { await expect(this.page.getByLabel(label, { exact: true })).toHaveValue(value); }
  async expectFocused(label: string) { await expect(this.page.getByLabel(label, { exact: true })).toBeFocused(); }
  async expectFieldError(label: string, text: string) {
    const field = this.page.getByLabel(label, { exact: true });
    await expect(field).toHaveAttribute('aria-invalid', 'true');
    await expect(this.page.getByRole('alert').filter({ hasText: text })).toBeVisible();
  }
  async expectCount(text: string) { await expect(this.page.getByText(text, { exact: true })).toBeVisible(); }
  async save() { await this.page.getByRole('button', { name: 'Save module', exact: true }).click(); }
  async expectSaved() { await this.expectStatus('Module saved.'); await expect(this.page.getByText('No unsaved changes.')).toBeVisible(); }
  async expectUnsaved(count: number) { await expect(this.page.getByText(`${count} field${count === 1 ? '' : 's'} changed and not yet saved.`)).toBeVisible(); }
  async discard() { await this.page.getByRole('button', { name: 'Discard changes', exact: true }).click(); }
  async expectRefused(count: number, ...items: string[]) {
    await this.expectStatus(`${count} fields were refused. Nothing was stored.`);
    const aside = this.page.getByRole('complementary', { name: 'What was refused' });
    for (const item of items) await expect(aside.getByRole('listitem').filter({ hasText: item })).toBeVisible();
  }
  // The new field is addressed by its position, so the fill waits for it rather than landing on the field before it.
  private steps() { return this.page.getByRole('list', { name: 'Practice steps' }).getByRole('textbox'); }
  async addStep(text: string) {
    const count = await this.steps().count();
    await this.page.getByRole('button', { name: 'Add step', exact: true }).click();
    await expect(this.steps()).toHaveCount(count + 1);
    await this.steps().last().fill(text);
  }
  async expectSteps(...texts: string[]) {
    const steps = this.page.getByRole('list', { name: 'Practice steps' }).getByRole('textbox');
    await expect(steps).toHaveCount(texts.length);
    for (const [index, text] of texts.entries()) await expect(steps.nth(index)).toHaveValue(text);
  }
  async moveStepUp(position: number, total: number) { await this.page.getByRole('button', { name: `Move practice step up from position ${position} of ${total}`, exact: true }).click(); }
  async expectStepCannotMove(position: number, total: number, direction: 'up' | 'down') {
    await expect(this.page.getByRole('button', { name: `Practice step ${position} of ${total} cannot move ${direction}`, exact: true })).toBeDisabled();
  }
  async removeStep(position: number, total: number) {
    await this.page.getByRole('button', { name: `Remove practice step ${position} of ${total}`, exact: true }).click();
    await expect(this.steps()).toHaveCount(total - 1);
  }
  private prompts() { return this.page.getByRole('list', { name: 'Preparation prompts' }).getByRole('textbox'); }
  async addPrompt(text: string) {
    const count = await this.prompts().count();
    await this.page.getByRole('button', { name: 'Add prompt', exact: true }).click();
    await expect(this.prompts()).toHaveCount(count + 1);
    await this.prompts().last().fill(text);
  }
  async expectPrompts(...texts: string[]) {
    const prompts = this.page.getByRole('list', { name: 'Preparation prompts' }).getByRole('textbox');
    await expect(prompts).toHaveCount(texts.length);
    for (const [index, text] of texts.entries()) await expect(prompts.nth(index)).toHaveValue(text);
  }
  async expectPromptBlocked(position: number, answers: string) {
    const row = this.page.getByRole('list', { name: 'Preparation prompts' }).getByRole('listitem').nth(position - 1);
    await expect(row.getByText('Removal blocked')).toBeVisible(); await expect(row.getByText(answers)).toBeVisible();
    await expect(row.getByRole('button', { name: /Remove prompt/ })).toHaveCount(0);
  }
  async removePromptAfterConfirming(position: number, total: number) {
    const trigger = this.page.getByRole('button', { name: `Remove prompt ${position} of ${total}`, exact: true });
    await trigger.click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove this prompt?' })).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Keep it' })).toBeFocused();
    await dialog.getByRole('button', { name: 'Remove prompt' }).click();
    await expect(dialog).not.toBeVisible();
  }
  private trailLink() { return this.page.getByRole('navigation', { name: 'Authoring trail' }).getByRole('link').first(); }
  async leaveViaTrail() { await this.trailLink().click(); }
  async expectLeft() { await expect(this.page).not.toHaveURL(/\/admin\/modules\//); }
  async leave(choice: 'Keep editing' | 'Discard changes') {
    await this.trailLink().click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Leave without saving?' })).toBeVisible();
    await dialog.getByRole('button', { name: choice }).click();
    await expect(dialog).not.toBeVisible();
  }
  async expectLeaveWarning(text: string) {
    await this.trailLink().click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByText(text)).toBeVisible();
    await dialog.getByRole('button', { name: 'Keep editing' }).click();
  }
  // The browser asks before a reload discards typed content; dismissing keeps the page.
  async reloadAndStay() {
    const asked = new Promise<void>(resolve => this.page.once('dialog', async dialog => { expect(dialog.type()).toBe('beforeunload'); await dialog.dismiss(); resolve(); }));
    // A dismissed warning cancels the navigation, so the reload never completes; a short timeout lets it settle.
    const reload = this.page.reload({ timeout: 3000 }).catch(() => undefined);
    await asked; await reload;
  }
  async expectConflict() {
    await expect(this.page.getByRole('heading', { name: 'This module changed since it was opened.' })).toBeVisible();
    await this.page.getByRole('button', { name: 'Show the current content', exact: true }).click();
  }
  async expectCurrentContent(title: string) { await expect(this.page.getByRole('definition').filter({ hasText: title })).toBeVisible(); }
  async reloadKeepingChanges() { await this.page.getByRole('button', { name: 'Reload and keep my changes', exact: true }).click(); }
  async expectSessionExpiredNotice() {
    await this.expectAlert('Your session expired. Your changes are still here.');
    await expect(this.page.getByRole('link', { name: 'Sign in in another tab' })).toBeVisible();
    await expect(this.page).toHaveURL(/\/admin\/modules\//);
  }
  async expectRemovalBlocked(reason: string) {
    await expect(this.page.getByRole('button', { name: 'Remove module', exact: true })).toBeDisabled();
    await expect(this.page.getByText(reason)).toBeVisible();
  }
  async removeAfterConfirming() {
    await this.page.getByRole('button', { name: 'Remove module', exact: true }).click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove this module?' })).toBeVisible();
    await dialog.getByRole('button', { name: 'Remove module' }).click();
  }
  async addSection(title: string, reading: string) {
    const start = this.page.getByRole('button', { name: 'Add section', exact: true });
    await start.click();
    await this.page.getByLabel('Section title', { exact: true }).fill(title); await this.page.getByLabel('Section reading', { exact: true }).fill(reading);
    await this.page.getByRole('group', { name: 'Add section' }).getByRole('button', { name: 'Add section', exact: true }).click();
  }
  async editSection(title: string) { await this.page.getByRole('link', { name: `Edit ${title}`, exact: true }).click(); }
  async expectSectionBlocked(position: number) {
    const row = this.page.getByRole('list', { name: 'Sections' }).getByRole('listitem').nth(position - 1);
    await expect(row.getByText('Removal blocked')).toBeVisible(); await expect(row.getByRole('button', { name: /Remove the section/ })).toHaveCount(0);
  }
  async removeSectionAfterConfirming(title: string) {
    const trigger = this.page.getByRole('button', { name: `Remove the section ${title}`, exact: true });
    await trigger.click();
    const dialog = this.page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Remove this section?' })).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Keep it' })).toBeFocused();
    await this.page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused();
    await trigger.click(); await dialog.getByRole('button', { name: 'Remove section' }).click();
  }
  async expectSectionCount(count: number) { await expect(this.page.getByRole('list', { name: 'Sections' }).getByRole('listitem')).toHaveCount(count); }
  async moveSectionDown(title: string, position: number, total: number) { await this.page.getByRole('button', { name: `Move ${title} down from position ${position} of ${total}`, exact: true }).click(); }
  async expectSectionOrder(...titles: string[]) {
    const rows = this.page.getByRole('list', { name: 'Sections' }).getByRole('listitem');
    for (const [index, title] of titles.entries()) await expect(rows.nth(index)).toContainText(title);
  }
  async movePromptUp(position: number, total: number) { await this.page.getByRole('button', { name: `Move prompt up from position ${position} of ${total}`, exact: true }).click(); }
  async openPreview() { await this.page.getByRole('link', { name: 'Preview as a participant', exact: true }).click(); }
  async backToEditor() { await this.page.getByRole('link', { name: '← Back to editor', exact: true }).click(); }
  async expectSectionRow(position: number, title: string, detail: string) {
    const row = this.page.getByRole('list', { name: 'Sections' }).getByRole('listitem').nth(position - 1);
    await expect(row).toContainText(title); await expect(row).toContainText(detail);
  }
}
