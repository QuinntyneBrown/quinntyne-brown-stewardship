import { expect } from '@playwright/test';
import { ScreenPage } from './screen-page';
export class SessionDetailPage extends ScreenPage {
  async open(path: string) { await this.page.goto(path); }
  async path() { return new URL(this.page.url()).pathname; }
  async expectPrompts() { await expect(this.page.getByRole('heading', { name: 'Before your session' })).toBeVisible(); }
  async answer() { await this.page.getByRole('link', { name: 'Write an answer', exact: true }).first().click(); }
  async expectAnswer(text: string) { await expect(this.page.getByText(text, { exact: true })).toBeVisible(); }
  async moreNotes() { await this.page.getByRole('button', { name: 'More notes', exact: true }).click(); }
  async expectNoteCount(count: number) { await expect(this.page.getByRole('link', { name: 'Edit note', exact: true })).toHaveCount(count); }
}
