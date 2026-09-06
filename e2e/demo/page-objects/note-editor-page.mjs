export class NoteEditorPage extends ShellPage {
  async write(body) {
    const input = this.page.getByRole('textbox', { name: 'Your note', exact: true });
    await input.fill(body); await this.pause(600);
  }
  async save(body) {
    const saved = this.page.waitForResponse(response => response.url().split('?')[0] === this.config.baseUrl + '/notes' && response.request().method() === 'POST' && response.status() === 200);
    await this.page.getByRole('button', { name: 'Save note', exact: true }).click();
    const note = await (await saved).json();
    if (note.body !== body) throw new Error('The live API did not persist the complete note body.');
    await this.page.getByText(body, { exact: true }).waitFor();
    return note;
  }
  async create() {
    if (!await this.page.getByRole('button', { name: 'Save note', exact: true }).isDisabled()) throw new Error('An empty note can be saved.');
    await this.write(this.config.note); this.page.__demo.note = await this.save(this.config.note);
  }
  async beginRevision() {
    await this.write(this.config.revisedNote);
    await this.page.getByRole('link', { name: 'Back to notes', exact: true }).click();
  }
  async finishRevision() {
    const value = await this.page.getByRole('textbox', { name: 'Your note', exact: true }).inputValue();
    if (value !== this.config.revisedNote) throw new Error('Declining navigation lost the unsaved draft.');
    await this.pause(900); this.page.__demo.note = await this.save(this.config.revisedNote);
    await this.page.reload(); await this.page.getByText(this.config.revisedNote, { exact: true }).waitFor();
    this.page.__demo.checks.push('A module note was created, revised, protected against unsaved navigation and recovered after reload.');
  }
}
