import { expect, Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

export class SignInPage {
  constructor(private readonly page: Page) {}
  async captureScreenshot(path: string) {
    await this.page.evaluate(() => document.fonts.ready);
    await this.page.screenshot({ path, fullPage: true });
  }
  async open() {
    await this.page.goto("/sign-in");
  }
  async signIn(
    email = "participant@example.com",
    password = "A private testing phrase 42!",
  ) {
    await this.page.getByLabel("Email address").fill(email);
    await this.page.getByLabel("Password", { exact: true }).fill(password);
    await this.page
      .getByRole("button", { name: "Sign in", exact: true })
      .click();
  }
  async expectVisible() {
    await expect(
      this.page.getByRole("heading", { name: "Sign in to continue" }),
    ).toBeVisible();
  }
  async expectHolding(path: string) {
    await expect(this.page.getByText("Holding:")).toContainText(path);
  }
  async expectNoAuthoredContent() {
    await expect(this.page.getByRole("main")).not.toContainText(/Stewardship — core|Foundations for founders|Redemptive practice intensive/);
  }
  // Field messages are announced too, so the server's message is picked out by its text.
  async expectError(message: string) {
    await expect(this.page.getByRole("alert").filter({ hasText: message })).toBeVisible();
  }
  async expectEmptyFields() {
    await this.page
      .getByRole("button", { name: "Sign in", exact: true })
      .click();
    await expect(
      this.page.getByText("Enter your email address."),
    ).toBeVisible();
    await expect(this.page.getByText("Enter your password.")).toBeVisible();
    await expect(this.page.getByLabel("Email address")).toBeFocused();
  }
  async signInByKeyboard() {
    await this.page.getByLabel("Email address").focus();
    await this.page.keyboard.type("participant@example.com");
    await this.page.keyboard.press("Tab");
    await this.page.keyboard.type("A private testing phrase 42!");
    await this.page.keyboard.press("Tab");
    await this.page.keyboard.press("Enter");
  }
  async expectAccessible() {
    expect(
      (
        await new AxeBuilder({ page: this.page })
          .withTags(["wcag2a", "wcag2aa", "wcag21aa"])
          .analyze()
      ).violations,
    ).toEqual([]);
  }
  async expectFitsViewport() {
    expect(
      await this.page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
  }
}
