export class CurriculumPage extends ShellPage {
  async ready(completed = 0) {
    await this.text(`${completed} of 12 complete`);
    if (await this.page.getByRole('list', { name: 'Module path' }).getByRole('listitem').count() !== 12) throw new Error('The curriculum path is incomplete.');
  }
  async overview() {
    await this.go('/curriculum'); await this.ready(); await this.pause(1200);
    await this.go('/modules/4'); await this.ready();
    await this.text('Complete the current module to unlock this module.');
  }
  async continue() { await this.page.getByRole('link', { name: 'Continue module', exact: true }).click(); }
  async nextSession() {
    await this.go('/curriculum'); await this.ready(1);
    const booking = this.page.__demo.booking;
    await this.text(`${booking.durationMinutes} minutes with ${booking.mentorName} · ${booking.timeZone}`);
    await this.page.getByRole('heading', { name: new Intl.DateTimeFormat('en-CA', { weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit', timeZoneName: 'short', timeZone: booking.timeZone }).format(new Date(booking.startsAt)) }).scrollIntoViewIfNeeded();
    await this.pause(800);
  }
  async completedProgramme() {
    await this.go('/curriculum'); await this.ready(12); await this.text('✓ Curriculum complete');
    await this.text('This cohort has ended. Your completed modules remain available.');
    if (await this.page.getByRole('link', { name: 'Continue module' }).count()) throw new Error('A completed curriculum still has a current module.');
    await this.pause(1800);
  }
}
