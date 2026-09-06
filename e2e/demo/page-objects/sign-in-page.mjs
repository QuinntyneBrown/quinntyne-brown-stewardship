export class SignInPage extends ShellPage {
  async credentials(email, password) {
    await this.page.getByLabel('Email address').fill(email);
    await this.page.getByLabel('Password', { exact: true }).fill(password);
    await this.page.getByRole('button', { name: 'Sign in', exact: true }).click();
  }
  async enter(email = this.config.email) {
    await this.credentials(email, this.config.password);
    await this.page.getByRole('button', { name: 'Sign out', exact: true }).waitFor();
  }
  async enterKeyboard() {
    await this.page.getByLabel('Email address').focus();
    await this.page.keyboard.type(this.config.email, { delay: 25 }); await this.page.keyboard.press('Tab');
    await this.page.keyboard.type(this.config.password, { delay: 15 }); await this.page.keyboard.press('Tab'); await this.page.keyboard.press('Enter');
    await this.page.getByRole('button', { name: 'Sign out', exact: true }).waitFor();
  }
  async validation() {
    await this.go('/modules/1');
    await this.page.getByRole('heading', { name: 'Sign in to continue' }).waitFor();
    await this.page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await this.text('Enter your email address.'); await this.text('Enter your password.'); await this.pause(700);
    await this.credentials(this.config.email, 'An intentionally incorrect password');
    await this.text('Email address or password is incorrect');
  }
  async awaiting() {
    await this.enter('awaiting@demo.invalid');
    await this.page.getByRole('heading', { name: 'You are not yet enrolled in a cohort.' }).waitFor();
    await this.pause(1500); await this.signOut();
  }
  async protectedAfterSignOut() {
    await this.signOut(); await this.page.goBack();
    await this.page.getByRole('heading', { name: 'Sign in to continue' }).waitFor();
    await this.go('/notes');
    await this.page.getByRole('heading', { name: 'Sign in to continue' }).waitFor();
    const response = await this.page.request.get(this.config.baseUrl + '/notes');
    if (response.status() !== 401) throw new Error('Signed-out API access was not refused.');
    this.page.__demo.checks.push('Signed-out notes API returns 401; browser history and direct URLs return to sign-in.');
  }
}
