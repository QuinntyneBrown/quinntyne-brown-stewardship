import { expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
export abstract class ScreenPage {
  constructor(protected readonly page: Page) {}
  async expectAccessible() {
    await this.page.evaluate(() => document.fonts.ready);
    expect((await new AxeBuilder({ page: this.page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze()).violations).toEqual([]);
  }
  async expectFitsViewport() { expect(await this.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true); }
  async expectTargets() {
    for (const target of await this.page.getByRole('button').all()) {
      const box = await target.boundingBox(); if (!box) continue;
      expect(box.width).toBeGreaterThanOrEqual(44); expect(box.height).toBeGreaterThanOrEqual(44);
    }
  }
  async capture(path: string) { await this.page.evaluate(() => document.fonts.ready); await this.page.screenshot({ path, fullPage: true }); }
  async expectError(text: string) { await expect(this.page.getByRole('alert')).toContainText(text); }
  async retry() { await this.page.getByRole('button', { name: 'Try again', exact: true }).click(); }
  async navigate(name: string) {
    const menu = this.page.getByRole('button', { name: 'Menu', exact: true });
    if (await menu.isVisible() && await menu.getAttribute('aria-expanded') === 'false') await menu.click();
    await this.page.getByRole('navigation', { name: 'Programme', exact: true }).getByRole('link', { name, exact: true }).click();
  }
}
