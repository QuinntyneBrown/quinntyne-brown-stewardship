import { test, expect } from "@playwright/test";
import { SignInPage } from "../page-objects/sign-in-page";
import { EnrollmentPage } from "../page-objects/enrollment-page";

test.beforeEach(async ({ page }) => {
  await page.route("**/authentication/**", (route) => {
    throw new Error(
      "Mocked browser tests must not reach authentication HTTP adapters.",
    );
  });
  await page.route("**/enrollment", (route) => {
    throw new Error(
      "Mocked browser tests must not reach enrollment HTTP adapters.",
    );
  });
});

// Traces to: L2-007 AC1–2; L2-004 AC1–3; L2-001 AC1.
for (const destination of [
  "/curriculum",
  "/modules/3",
  "/sessions",
  "/notes",
]) {
  test(`sign-in preserves ${destination} and shows the enrollment notice`, async ({
    page,
  }) => {
    const signIn = new SignInPage(page);
    const enrollment = new EnrollmentPage(page);
    await enrollment.open(destination);
    await signIn.expectVisible();
    await signIn.signIn();
    await enrollment.expectNotice();
    await enrollment.expectDestination(destination);
    await enrollment.expectFitsViewport();
  });
}

// Traces to: L2-001 AC2–4.
test("empty fields and invalid credentials give clear feedback", async ({
  page,
}) => {
  const signIn = new SignInPage(page);
  await signIn.open();
  await signIn.expectEmptyFields();
  await signIn.signIn("participant@example.com", "wrong");
  await signIn.expectError("Email address or password is incorrect");
  await signIn.signIn("unknown@example.com", "wrong");
  await signIn.expectError("Email address or password is incorrect");
});

// Traces to: L2-038 AC1–2 (presentation).
test("a throttled sign-in displays the cooling-off time", async ({ page }) => {
  await page.addInitScript(() =>
    localStorage.setItem("stewardship.mock.throttled", "true"),
  );
  const signIn = new SignInPage(page);
  await signIn.open();
  await signIn.signIn();
  await signIn.expectError("Too many attempts");
});

// Traces to: L2-007 supporting failure paths.
test("enrollment failure can be retried", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("stewardship.mock.session", "true");
    localStorage.setItem("stewardship.mock.failOnce", "true");
  });
  const enrollment = new EnrollmentPage(page);
  await enrollment.open();
  await enrollment.expectFailure();
  await enrollment.retry();
  await enrollment.expectNotice();
});

// Traces to: L2-007 supporting loading state.
test("enrollment loading is announced", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("stewardship.mock.session", "true");
    localStorage.setItem("stewardship.mock.delay", "1000");
  });
  const enrollment = new EnrollmentPage(page);
  await enrollment.open();
  await enrollment.expectLoading();
  await enrollment.expectNotice();
});

// Traces to: L2-002 AC1–3, L2-003 AC1–2.
test("sessions survive reload and sign-out protects browser history", async ({
  page,
}) => {
  const signIn = new SignInPage(page);
  const enrollment = new EnrollmentPage(page);
  await signIn.open();
  await signIn.signIn();
  await enrollment.expectNotice();
  await enrollment.reload();
  await enrollment.expectNotice();
  await enrollment.navigate("Sessions");
  await enrollment.signOut();
  await signIn.expectVisible();
  await enrollment.back();
  await signIn.expectVisible();
});

// Traces to: L2-002 AC3.
test("an expired session requires sign-in again", async ({ page }) => {
  const signIn = new SignInPage(page);
  const enrollment = new EnrollmentPage(page);
  await signIn.open();
  await signIn.signIn();
  await enrollment.expectNotice();
  await enrollment.expireSession();
  await signIn.expectVisible();
});

// Traces to: L2-029–034, limited to implemented screens.
test("the journey is accessible and works by keyboard", async ({ page }, testInfo) => {
  const signIn = new SignInPage(page);
  const enrollment = new EnrollmentPage(page);
  await signIn.open();
  await signIn.expectAccessible();
  await signIn.captureScreenshot(testInfo.outputPath("sign-in.png"));
  await signIn.signInByKeyboard();
  await enrollment.expectNotice();
  await enrollment.expectAccessible();
  await enrollment.captureScreenshot(testInfo.outputPath("awaiting-enrollment.png"));
  for (const name of ["Sessions", "Notes", "Curriculum"]) {
    await enrollment.navigate(name);
    await enrollment.expectNotice();
  }
});

// Traces to: L2-004 AC3; only safe internal return URLs are accepted.
test("external return URLs fall back to curriculum", async ({ page }) => {
  await page.goto("/sign-in?returnUrl=https%3A%2F%2Fexample.org");
  await new SignInPage(page).signIn();
  await new EnrollmentPage(page).expectDestination("/curriculum");
});

// Traces to: L2-029 AC4–5, L2-030 AC1/3, L2-032 AC1; slice screens only.
test("controls remain reachable across all five breakpoints", async ({
  page,
}) => {
  const signIn = new SignInPage(page);
  const enrollment = new EnrollmentPage(page);
  await signIn.open();
  for (const width of [320, 576, 768, 992, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await signIn.expectFitsViewport();
  }
  await signIn.signIn();
  await enrollment.expectNotice();
  for (const width of [320, 576, 768, 992, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await enrollment.expectFitsViewport();
    await enrollment.expectTouchTargets();
  }
});

// Traces to: L2-003 supporting network failure: do not falsely claim revocation.
test("a failed sign-out leaves a retryable authenticated screen", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem("stewardship.mock.signOutFailure", "true"),
  );
  const signIn = new SignInPage(page);
  const enrollment = new EnrollmentPage(page);
  await signIn.open();
  await signIn.signIn();
  await enrollment.expectNotice();
  await enrollment.signOut();
  await enrollment.expectSignOutFailure();
  await enrollment.expectNotice();
  await enrollment.allowSignOut();
  await enrollment.signOut();
  await signIn.expectVisible();
});

// Traces to: L2-002 AC3; expiry between guard validation and enrollment loading.
test("expiry during the enrollment request returns to sign-in", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem("stewardship.mock.session", "true");
    localStorage.setItem("stewardship.mock.enrollmentExpired", "true");
  });
  await new EnrollmentPage(page).open("/sessions");
  await new SignInPage(page).expectVisible();
});

// Traces to: L2-004 supporting session-service failure.
test("a session-service failure offers sign-in without showing enrollment", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem("stewardship.mock.sessionFailure", "true"),
  );
  await new EnrollmentPage(page).open();
  const signIn = new SignInPage(page);
  await signIn.expectVisible();
  await signIn.expectError("We could not check your session");
});
