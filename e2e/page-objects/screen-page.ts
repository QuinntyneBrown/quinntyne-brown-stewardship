import { expect, Locator, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
export abstract class ScreenPage {
  constructor(protected readonly page: Page) {}
  async expectAccessible() {
    await this.page.evaluate(() => document.fonts.ready);
    expect((await new AxeBuilder({ page: this.page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze()).violations).toEqual([]);
  }
  async expectFitsViewport() { expect(await this.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true); }
  async expectTargets() {
    const targets = this.page.locator('button, a:not(p a), input:not([type="hidden"]), select, textarea');
    const boxes: { x: number; y: number; width: number; height: number }[] = [];
    for (const target of await targets.all()) {
      if (!await target.isVisible()) continue;
      if (await target.evaluate(element => element.getBoundingClientRect().right < 0)) continue;
      const box = await target.boundingBox(); if (!box) continue;
      expect(box.width).toBeGreaterThanOrEqual(44); expect(box.height).toBeGreaterThanOrEqual(44);
      for (const previous of boxes) {
        const overlap = Math.min(box.x + box.width, previous.x + previous.width) - Math.max(box.x, previous.x) > 0.5
          && Math.min(box.y + box.height, previous.y + previous.height) - Math.max(box.y, previous.y) > 0.5;
        expect(overlap).toBe(false);
      }
      boxes.push(box);
    }
  }
  protected async activateByKeyboard(target: Locator) {
    for (let index = 0; index < 100; index++) {
      if (await target.evaluate(element => element === document.activeElement)) {
        await expect(target).toBeFocused();
        expect(await target.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe('none');
        await this.page.keyboard.press('Enter'); return;
      }
      await this.page.keyboard.press('Tab');
    }
    throw new Error('The control was unreachable using Tab.');
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
