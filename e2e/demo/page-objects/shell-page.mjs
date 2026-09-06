export class ShellPage {
  constructor(page, config) { this.page = page; this.config = config; }
  async text(value) { await this.page.getByText(value, { exact: true }).first().waitFor(); }
  async go(path) { await this.page.goto(this.config.baseUrl + path); }
  async pause(ms = 500) { await this.page.waitForTimeout(ms); }
  async navigate(name) {
    const menu = this.page.getByRole('button', { name: 'Menu', exact: true });
    if (await menu.isVisible() && await menu.getAttribute('aria-expanded') === 'false') await menu.click();
    await this.page.getByRole('navigation', { name: 'Programme', exact: true }).getByRole('link', { name, exact: true }).click();
  }
  async signOut() {
    await this.page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await this.page.getByRole('heading', { name: 'Sign in to continue' }).waitFor();
  }
  async fits() {
    if (!await this.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)) throw new Error('The live page overflows its viewport.');
  }
}
