import { ScreenPage } from './screen-page';
import { expect, Page } from '@playwright/test';
export class NotesPage extends ScreenPage {
  constructor(page: Page) { super(page); }
  async open() { await this.page.goto('/notes'); }
  async add() { await this.page.getByRole('link', { name: 'Write a note', exact: true }).click(); }
  async expectNote(body: string) { await expect(this.page.getByText(body, { exact: true })).toBeVisible(); }
  async editFirst() { await this.page.getByRole('link', { name: 'Edit note', exact: true }).first().click(); }
}

