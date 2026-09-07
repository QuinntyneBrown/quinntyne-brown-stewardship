import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class ModulePage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open(ordinal?: number) { await this.page.goto(`/modules/${ordinal ?? 'current'}`); }
  async completeSection() { await this.page.getByRole('button', { name: 'Mark section complete' }).click(); }
  async expectSection(position: number, total = 5) { await expect(this.page.getByRole('progressbar', { name: `Section ${position} of ${total}`, exact: true })).toBeVisible(); }
  async expectProgress(completed: number, position: number, total = 5) {
    const progress = this.page.getByRole('progressbar', { name: `Section ${position} of ${total}`, exact: true });
    await expect(progress).toBeVisible();
    await expect(progress).toHaveJSProperty('position', completed / total);
  }
  // The reading is shown as typed: markup stays literal and a blank line starts a paragraph.
  async expectReading(title: string, ...paragraphs: string[]) {
    await expect(this.page.getByRole('heading', { level: 2, name: title })).toBeVisible();
    const article = this.page.getByRole('article');
    for (const paragraph of paragraphs) await expect(article.getByText(paragraph, { exact: true })).toBeVisible();
    expect(await article.locator('p.reading').count()).toBe(paragraphs.length);
  }
  async choose(position: number) { await this.page.getByRole('navigation', { name: 'Module sections' }).getByRole('button').nth(position - 1).click(); }
  async expectNoCompletionAction() {
    await expect(this.page.getByRole('button', { name: 'Mark section complete' })).toHaveCount(0);
    await expect(this.page.getByRole('link', { name: /Write an answer|Add a module note/ })).toHaveCount(0);
    await expect(this.page.getByText('✓ Section complete')).toHaveCount(0);
  }
  async expectEverySectionSelectable() {
    for (const button of await this.page.getByRole('navigation', { name: 'Module sections' }).getByRole('button').all()) await expect(button).toBeEnabled();
  }
  async expectHeading(title: string) { await expect(this.page.getByRole('heading', { level: 1, name: title })).toBeVisible(); }
  // The practice block reads exactly as authored: the effort estimate and the steps in their order, or nothing at all.
  async expectPractice(effort: string, ...steps: string[]) {
    await expect(this.page.getByText(`Put it into practice · ${effort}`)).toBeVisible();
    const items = this.page.getByRole('article').locator('ol.practice li');
    await expect(items).toHaveCount(steps.length);
    for (const [index, step] of steps.entries()) await expect(items.nth(index)).toHaveText(step);
  }
  async expectNoPractice() { await expect(this.page.getByRole('heading', { name: 'Your assignment' })).toHaveCount(0); await expect(this.page.getByText('Put it into practice')).toHaveCount(0); }
  async reload() { await this.page.reload(); }
  async addNote() { await this.page.getByRole('link', { name: 'Add a module note' }).click(); }
  async moreNotes() { await this.page.getByRole('button', { name: 'More notes', exact: true }).click(); }
  async expectNoteCount(count: number) { await expect(this.page.getByRole('link', { name: 'Edit note', exact: true })).toHaveCount(count); }
  async expectSameAssignmentAcrossViewports() {
    const viewport = this.page.viewportSize();
    const content = this.page.getByRole('article');
    await this.page.setViewportSize({ width: 320, height: 900 });
    const small = await content.innerText();
    await expect(content.getByRole('listitem')).toHaveCount(3);
    await this.page.setViewportSize({ width: 1440, height: 900 });
    expect(await content.innerText()).toBe(small);
    await expect(content.getByRole('listitem')).toHaveCount(3);
    if (viewport) await this.page.setViewportSize(viewport);
  }
  async expectSectionStateContrast() {
    const ratios = await this.page.getByRole('navigation', { name: 'Module sections' }).locator('button:disabled span').evaluateAll(labels => {
      const rgb = (value: string) => value.match(/[\d.]+/g)!.slice(0, 3).map(Number);
      const luminance = (values: number[]) => values.map(value => {
        const channel = value / 255;
        return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
      }).reduce((sum, value, index) => sum + value * [0.2126, 0.7152, 0.0722][index], 0);
      return labels.map(label => {
        const background = rgb(getComputedStyle(document.body).backgroundColor);
        const foreground = rgb(getComputedStyle(label).color);
        let opacity = 1;
        for (let node: Element | null = label; node; node = node.parentElement) opacity *= Number(getComputedStyle(node).opacity);
        const text = foreground.map((channel, index) => channel * opacity + background[index] * (1 - opacity));
        const a = luminance(text), b = luminance(background);
        return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
      });
    });
    expect(ratios.length).toBeGreaterThan(0);
    for (const ratio of ratios) expect(ratio).toBeGreaterThanOrEqual(4.5);
  }
}

