export class PreparationPage extends ShellPage {
  async open() {
    await this.go('/sessions/' + this.page.__demo.booking.id);
    await this.page.getByRole('heading', { name: 'Before your session', exact: true }).waitFor();
  }
  async answer() { await this.page.getByRole('link', { name: 'Write an answer', exact: true }).first().click(); }
  async checkAnswer() { await this.open(); await this.text(this.config.answer); }
  async addNote() { await this.page.getByRole('link', { name: 'Add a session note', exact: true }).click(); }
  async checkNote() {
    await this.open(); await this.text(this.config.sessionNote);
    await this.page.getByRole('heading', { name: 'Session notes', exact: true }).scrollIntoViewIfNeeded();
    this.page.__demo.checks.push('Preparation prompts retain their saved answers and session notes appear against their booking.');
  }
}
