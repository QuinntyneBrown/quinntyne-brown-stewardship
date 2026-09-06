import { Page } from "@playwright/test";
import type { MockState } from "@qbs/api/testing";

// The acceptance host binds the service tokens to mocks and installs a bridge on the
// page. This object owns how a test reaches that bridge, so a spec states the
// condition it needs and never touches the mocks' own state.
export class MockBridge {
  constructor(private readonly page: Page) {}

  signedIn() {
    return this.seed({ signedIn: true });
  }
  throttledSignIn() {
    return this.seed({ throttled: true });
  }
  failingSessionCheck() {
    return this.seed({ sessionFailure: true });
  }
  failingSignOut() {
    return this.seed({ signOutFailure: true });
  }
  expiredEnrollment() {
    return this.seed({ enrollmentExpired: true });
  }
  enrollmentFailsOnce() {
    return this.seed({ enrollmentFailsOnce: true });
  }
  slowEnrollment(milliseconds: number) {
    return this.seed({ enrollmentDelayMs: milliseconds });
  }
  expireSession() {
    return this.apply({ signedIn: false });
  }
  allowSignOut() {
    return this.apply({ signOutFailure: false });
  }

  // Seeding runs before the application bootstraps, so the state is in place on the
  // first render and survives a reload.
  private async seed(changes: Partial<MockState>) {
    await this.page.addInitScript((seeded) => {
      const bridge = (window.__stewardship ??= {});
      bridge.seed = { ...bridge.seed, ...seeded };
    }, changes);
  }

  // Applying steers an application that is already running.
  private async apply(changes: Partial<MockState>) {
    await this.page.evaluate(
      (applied) => window.__stewardship!.configure!(applied),
      changes,
    );
  }
}
