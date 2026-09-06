import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class NotesPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/notes'); }
  async expectReady() { await expect(this.page.getByText('No notes yet.', { exact: false })).toBeVisible(); }
  async add() { await this.page.getByRole('link', { name: 'Write a note', exact: true }).click(); }
  async expectNote(body: string) { await expect(this.page.getByText(body, { exact: true })).toBeVisible(); }
  async editFirst() { await this.page.getByRole('link', { name: 'Edit note', exact: true }).first().click(); }
  async expectVisibleCount(count: number) { await expect(this.page.getByRole('link', { name: 'Edit note', exact: true })).toHaveCount(count); }
  async more() { await this.page.getByRole('button', { name: 'More notes', exact: true }).click(); }
  async expectNoMore() { await expect(this.page.getByRole('button', { name: 'More notes', exact: true })).toHaveCount(0); }
}

