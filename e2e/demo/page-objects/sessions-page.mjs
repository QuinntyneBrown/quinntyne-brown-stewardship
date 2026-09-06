export class SessionsPage extends ShellPage {
  async ready() { await this.page.getByRole('heading', { name: 'Sit with your mentor' }).waitFor(); }
  async canBook() { await this.page.getByRole('button', { name: 'Confirm booking', exact: true }).waitFor(); }
  async history() {
    await this.go('/sessions'); await this.canBook();
    const notes = this.page.getByRole('link', { name: 'Session notes', exact: true });
    if (await notes.count() !== 2) throw new Error('The two historical sessions are missing.');
    await notes.first().click(); await this.text('Show all notes');
    await this.text('Conversation reflection: listen to the people carrying the cost before choosing a technical solution.');
    await this.pause(800); await this.go('/sessions'); await this.canBook(); await this.publishedDay();
  }
  async publishedDay() {
    const name = new Intl.DateTimeFormat('en-CA', { weekday: 'short', month: 'short', day: 'numeric', timeZone: 'UTC' }).format(new Date(this.config.firstDay + 'T12:00:00Z'));
    let target = this.page.getByRole('group', { name: 'Booking days' }).getByRole('button', { name, exact: true });
    if (!await target.count()) { await this.page.getByRole('button', { name: 'Next week', exact: true }).click(); await target.waitFor(); }
    await target.click(); await this.page.getByRole('button', { name: /Taken ·/ }).first().waitFor();
    if (!await this.page.getByRole('button', { name: /Taken ·/ }).first().isDisabled()) throw new Error('A taken slot is selectable.');
    await this.page.getByRole('button', { name: /Open ·/ }).first().waitFor();
  }
  async book() {
    const button = this.page.getByRole('button', { name: 'Confirm booking', exact: true });
    if (!await button.isDisabled()) throw new Error('Booking is offered before selecting a slot.');
    await this.page.getByRole('button', { name: /Open ·/ }).first().click(); await this.pause(700);
    const response = this.page.waitForResponse(r => r.url().split('?')[0] === this.config.baseUrl + '/sessions' && r.request().method() === 'POST' && r.status() === 200);
    await button.click(); this.page.__demo.booking = await (await response).json();
    await this.page.getByRole('heading', { name: 'Your next session', exact: true }).waitFor();
    await this.text('3 of 6 booked · Quinntyne Brown');
  }
  async changeAndCancel() {
    await this.go('/sessions'); await this.page.getByRole('button', { name: 'Reschedule', exact: true }).click();
    await this.publishedDay(); await this.page.getByRole('button', { name: /Open ·/ }).first().click();
    const response = this.page.waitForResponse(r => /\/sessions\/[^/]+\/slot$/.test(r.url()) && r.request().method() === 'PUT' && r.status() === 200);
    await this.page.getByRole('button', { name: 'Confirm change', exact: true }).click();
    const changed = await (await response).json();
    if (changed.slotId === this.page.__demo.booking.slotId) throw new Error('Rescheduling did not change the booking.');
    await this.page.getByRole('button', { name: 'Cancel session', exact: true }).click();
    await this.page.getByRole('dialog').getByRole('button', { name: 'Keep session', exact: true }).click(); await this.pause(700);
    await this.page.getByRole('button', { name: 'Cancel session', exact: true }).click();
    await this.page.getByRole('dialog').getByRole('button', { name: 'Yes, cancel session', exact: true }).click();
    await this.canBook(); await this.text('2 of 6 booked · Quinntyne Brown');
    this.page.__demo.checks.push('Booking, curriculum next-session details, rescheduling and cancellation used the live API; cancellation restored the allowance.');
  }
  async cutoff() {
    await this.go('/sessions'); const change = this.page.getByRole('button', { name: 'Reschedule', exact: true }); await change.waitFor();
    if (!await change.isDisabled() || !await this.page.getByRole('button', { name: 'Cancel session', exact: true }).isDisabled()) throw new Error('The 24-hour change cutoff is not enforced in the UI.');
    await this.text('Changes close 24 hours before the session starts.'); await this.pause(1000);
  }
  async ended() {
    await this.go('/sessions'); await this.text('6 of 6 booked · Quinntyne Brown');
    await this.text('This cohort has ended. New bookings are unavailable.');
    if (await this.page.getByRole('button', { name: 'Confirm booking', exact: true }).count()) throw new Error('An ended cohort can book another session.');
  }
}
