import { expect, Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
export class CataloguePage {
  constructor(private readonly page: Page) {}
  async open() {
    await this.page.goto("/");
  }
  async expectNotice() {
    await expect(
      this.page.getByRole("heading", {
        name: "You are not yet enrolled in a cohort.",
      }),
    ).toBeVisible();
  }
  async useKeyboard() {
    await this.page.getByLabel("Email address").focus();
    await this.page.keyboard.type("participant@example.com");
    await this.page.keyboard.press("Tab");
    const button = this.page.getByRole("button", {
      name: "Preview confirmation",
    });
    await expect(button).toBeFocused();
    expect(
      await button.evaluate(
        (element) => getComputedStyle(element).outlineStyle,
      ),
    ).toBe("solid");
    await this.page.keyboard.press("Enter");
    await expect(this.page.getByRole("status")).toHaveText(
      "Confirmation sent to participant@example.com",
    );
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
