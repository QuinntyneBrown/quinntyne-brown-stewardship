import { expect, Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

export class EnrollmentPage {
  constructor(private readonly page: Page) {}
  async captureScreenshot(path: string) {
    await this.page.evaluate(() => document.fonts.ready);
    await this.page.screenshot({ path, fullPage: true });
  }
  async open(path = "/curriculum") {
    await this.page.goto(path);
  }
  async expectNotice() {
    await expect(
      this.page.getByRole("heading", {
        name: "You are not yet enrolled in a cohort.",
      }),
    ).toBeVisible();
    await expect(this.page.getByRole("main")).not.toContainText(
      /\d+ of 12 complete|modules remain|Book a session/,
    );
    await expect(
      this.page.getByRole("list", { name: "Module path" }),
    ).toHaveCount(0);
  }
  async expectDestination(path: string) {
    await expect(this.page).toHaveURL(
      new RegExp(path.replaceAll("/", "\\/") + "$"),
    );
  }
  async navigate(name: string) {
    await this.page
      .getByRole("navigation")
      .getByRole("link", { name, exact: true })
      .click();
  }
  async signOut() {
    await this.page
      .getByRole("button", { name: "Sign out", exact: true })
      .click();
  }
  async back() {
    await this.page.goBack();
  }
  async reload() {
    await this.page.reload();
  }
  async expectLoading() {
    await expect(this.page.getByRole("status")).toContainText(
      "Checking your enrollment",
    );
  }
  async expectFailure() {
    await expect(this.page.getByRole("alert")).toContainText(
      "We could not check your enrollment",
    );
  }
  async retry() {
    await this.page.getByRole("button", { name: "Try again" }).click();
  }
  async expireSession() {
    await this.page.evaluate(() =>
      localStorage.removeItem("stewardship.mock.session"),
    );
    await this.page.reload();
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
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
  }
  async expectTouchTargets() {
    for (const target of await this.page
      .getByRole("navigation")
      .getByRole("link")
      .all()) {
      const bounds = await target.boundingBox();
      expect(bounds!.width).toBeGreaterThanOrEqual(44);
      expect(bounds!.height).toBeGreaterThanOrEqual(44);
    }
  }
  async expectSignOutFailure() {
    await expect(this.page.getByRole("alert")).toContainText(
      "We could not sign you out",
    );
  }
  async allowSignOut() {
    await this.page.evaluate(() =>
      localStorage.removeItem("stewardship.mock.signOutFailure"),
    );
  }
}
