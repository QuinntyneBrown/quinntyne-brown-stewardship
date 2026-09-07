import { Page } from "@playwright/test";
import type { MockState, ProgrammeShape } from "@qbs/api/testing";

// The acceptance host binds the service tokens to mocks and installs a bridge on the
// page. This object owns how a test reaches that bridge, so a spec states the
// condition it needs and never touches the mocks' own state.
export class MockBridge {
  constructor(private readonly page: Page) {}

  signedIn() {
    return this.seed({ signedIn: true });
  }
  // An administrator; a spec that needs the first programme in another shape states it.
  async administrator(shape?: Partial<ProgrammeShape>) {
    await this.seed({ signedIn: true, administrator: true });
    if (shape) await this.shape(shape);
  }
  // An administrator who is also enrolled, so one tab can author a change and read it as a participant.
  async administratorEnrolled(shape?: Partial<ProgrammeShape>) {
    await this.seed({ signedIn: true, administrator: true, enrolled: true });
    if (shape) await this.shape(shape);
  }
  // The next reorder is refused, as if the list had changed elsewhere.
  refuseReorder() {
    return this.seed({ reorderRefused: true });
  }
  // The next module save finds the module revised elsewhere.
  staleRevision() {
    return this.seed({ staleRevision: true });
  }
  // A participant has answered the first prompt of the first module.
  async answeredPrompt() {
    await this.page.addInitScript(() => {
      const bridge = (window.__stewardship ??= {});
      bridge.programmeSeed = { ...bridge.programmeSeed, notes: [...(bridge.programmeSeed?.notes ?? []), {
        id: 'answer-1', moduleId: 'module-1', sessionId: null, promptId: 'prompt-1', attachmentTitle: 'Begin with stewardship',
        body: 'A colleague who maintains the service.', revisedAt: '2026-09-01T09:00:00.000Z', revision: 'answer-revision-1', canEdit: true,
      }] };
    });
  }
  // Sections a participant has already marked complete.
  async completedSections(...ids: string[]) {
    await this.page.addInitScript((completed) => {
      const bridge = (window.__stewardship ??= {});
      bridge.programmeSeed = { ...bridge.programmeSeed, completed: [...(bridge.programmeSeed?.completed ?? []), ...completed] };
    }, ids);
  }
  // An administrator account that has not signed in on this device yet.
  administratorSignedOut() {
    return this.seed({ signedIn: false, administrator: true });
  }
  async noProgrammes() {
    await this.page.addInitScript(() => {
      const bridge = (window.__stewardship ??= {});
      bridge.programmeSeed = { ...bridge.programmeSeed, noProgrammes: true };
    });
  }
  // A spec that needs a programme or cohort of another size states it; the mocks derive every figure from it.
  async enrolled(shape?: Partial<ProgrammeShape>) {
    await this.seed({ signedIn: true, enrolled: true });
    if (shape) await this.shape(shape);
  }
  private async shape(shape: Partial<ProgrammeShape>) {
    await this.page.addInitScript((seeded) => {
      const bridge = (window.__stewardship ??= {});
      bridge.programmeSeed = { ...bridge.programmeSeed, shape: seeded };
    }, shape);
  }
  async longNotes(sessionNotes = false) {
    await this.page.addInitScript((sessionNotes) => {
      const bridge = window.__stewardship ??= {};
      bridge.programmeSeed = { ...bridge.programmeSeed, ...(sessionNotes ? { booking: {
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
