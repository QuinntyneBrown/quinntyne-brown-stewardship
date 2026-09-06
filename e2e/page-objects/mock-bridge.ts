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
  enrolled() {
    return this.seed({ signedIn: true, enrolled: true });
  }
  async longNotes(sessionNotes = false) {
    await this.page.addInitScript((sessionNotes) => {
      const bridge = window.__stewardship ??= {};
      bridge.programmeSeed = { ...(sessionNotes ? { booking: {
        id: 'session-long-notes', slotId: 'slot-2026-09-10-14', startsAt: '2026-09-10T14:00:00Z',
        durationMinutes: 45, mentorName: 'Quinntyne Brown', timeZone: 'America/Toronto', status: 'Booked',
        canChange: true, changeReason: null, moduleOrdinal: 1, moduleTitle: 'Begin with stewardship',
      } } : {}), notes: Array.from({ length: 4 }, (_, i) => ({
        id: `long-note-${i}`, moduleId: sessionNotes ? null : 'module-1', sessionId: sessionNotes ? 'session-long-notes' : null, promptId: null,
        attachmentTitle: 'Begin with stewardship', body: `Reflection ${i}: ` + 'A'.repeat(9900),
        revisedAt: new Date(Date.UTC(2026, 8, 10 - i)).toISOString(), revision: `revision-${i}`, canEdit: true,
      })) };
    }, sessionNotes);
  }
  unavailableSlot() {
    return this.seed({ slotConflict: true });
  }
  failingProgramme() {
    return this.seed({ programmeFailure: true });
  }
  allowProgramme() {
    return this.apply({ programmeFailure: false });
  }
  interruptProgramme() {
    return this.apply({ programmeFailure: true });
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
  slowResponses(milliseconds: number) {
    return this.seed({ responseDelayMs: milliseconds });
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
