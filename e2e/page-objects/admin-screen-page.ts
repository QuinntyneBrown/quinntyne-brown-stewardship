import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
// What every authoring screen shares: the trail back to the programme index and the regions a save or a refusal reports into.
export abstract class AdminScreenPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async expectTrail(...labels: string[]) {
    const trail = this.page.getByRole('navigation', { name: 'Authoring trail' });
    for (const label of labels) await expect(trail.getByText(label, { exact: true }).first()).toBeAttached();
  }
  // The accessible names of every action the screen offers, whatever the viewport condenses or collapses.
  async actionNames() {
    const menu = this.page.getByRole('button', { name: 'Menu', exact: true });
    if (await menu.isVisible() && await menu.getAttribute('aria-expanded') === 'false') { await menu.click(); await expect(menu).toHaveAttribute('aria-expanded', 'true'); }
    await expect(this.page.getByRole('navigation', { name: 'Programme', exact: true }).getByRole('link').first()).toBeVisible();
    const names = await this.page.locator('button, a').evaluateAll(elements => elements.filter(e => !e.closest('nav[aria-label="Authoring trail"]') && ((e as HTMLElement).offsetParent !== null || getComputedStyle(e).position === 'fixed')).map(e => (e.getAttribute('aria-label') ?? e.textContent ?? '').replace(/\s+/g, ' ').trim()).filter(Boolean));
    // The menu toggle exists for one viewport only, and the trail is wayfinding rather than an action; every authoring action must exist for both.
    return new Set(names.filter(n => n !== 'Menu'));
  }
  async expectCondensedTrail(parent: string) {
    const trail = this.page.getByRole('navigation', { name: 'Authoring trail' });
    const link = trail.getByRole('link', { name: `Authoring · ${parent}`, exact: true });
    await expect(link).toBeVisible();
    const box = await link.boundingBox(); expect(box && box.height).toBeGreaterThanOrEqual(44);
    await expect(trail.getByRole('list')).toBeHidden();
  }
  async expectFullTrail() {
    const trail = this.page.getByRole('navigation', { name: 'Authoring trail' });
    await expect(trail.getByRole('list')).toBeVisible();
    await expect(trail.getByRole('link', { name: /^Authoring · / })).toBeHidden();
  }
  async expectStateAsText(state: 'Draft' | 'Published') { await expect(this.page.getByText(`Publication state: ${state}`).first()).toBeAttached(); }
  async expectAlert(text: string) { await expect(this.page.getByRole('alert').filter({ hasText: text })).toBeVisible(); }
  async expectStatus(text: string) { await expect(this.page.getByRole('status').filter({ hasText: text })).toBeVisible(); }
}
