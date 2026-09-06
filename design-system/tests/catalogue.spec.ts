import { test } from "@playwright/test";
import { CataloguePage } from "./catalogue-page";
// Traces to: AE-10; L2-029/031/032/033, design-system specimens for this slice.
for (const width of [390, 1440]) {
  test(`notice and controls are usable at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    const catalogue = new CataloguePage(page);
    await catalogue.open();
    await catalogue.expectNotice();
    await catalogue.useKeyboard();
    await catalogue.expectAccessible();
    await catalogue.expectFitsViewport();
  });
}
