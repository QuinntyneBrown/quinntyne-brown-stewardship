import { expect, test } from "@playwright/test";
import { BrowserMeasurement } from "../page-objects/browser-measurement";
import { ProductionResponses } from "../page-objects/production-responses";
import { CurriculumPage } from "../page-objects/curriculum-page";
import { ModulePage } from "../page-objects/module-page";
import { SessionsPage } from "../page-objects/sessions-page";
import { NotesPage } from "../page-objects/notes-page";
import { NoteEditorPage } from "../page-objects/note-editor-page";
import { SessionDetailPage } from "../page-objects/session-detail-page";
import { SignInPage } from "../page-objects/sign-in-page";
import { EnrollmentPage } from "../page-objects/enrollment-page";
import { AdminProgrammesPage } from "../page-objects/admin-programmes-page";
import { AdminProgrammePage } from "../page-objects/admin-programme-page";
import { AdminModulePage } from "../page-objects/admin-module-page";
import { AdminSectionPage } from "../page-objects/admin-section-page";

// Traces to: L2-039 AC3–4. Given a cold browser with a repeatable mid-tier/4G
// lab profile, when each screen loads, then its complete compressed transfer is
// at most 300KB and curriculum controls work within 2.5 seconds.
for (const screen of [
  "sign-in",
  "not-enrolled",
  "curriculum",
  "module",
  "sessions",
  "notes",
  "note-editor",
  "note-edit",
  "session-detail",
  "admin-programmes",
  "admin-programme",
  "admin-module",
  "admin-section",
]) {
  test(`${screen} meets its cold-load budget`, async ({ page }, info) => {
    const production = new ProductionResponses(page);
    const fixture = await production.install(screen);
    const curriculum = new CurriculumPage(page),
      module = new ModulePage(page),
      sessions = new SessionsPage(page);
    const notes = new NotesPage(page),
      editor = new NoteEditorPage(page),
      detail = new SessionDetailPage(page);
    const measurement = new BrowserMeasurement(page);
    await measurement.configure();
    const actions: Record<
      string,
      [() => Promise<unknown>, () => Promise<unknown>]
    > = {
      "sign-in": [
        () => new SignInPage(page).open(),
        () => new SignInPage(page).expectVisible(),
      ],
      "not-enrolled": [
        () => new EnrollmentPage(page).open(),
        () => new EnrollmentPage(page).expectNotice(),
      ],
      curriculum: [() => curriculum.open(), () => curriculum.expectReady()],
      module: [() => module.open(), () => module.expectSection(1)],
      sessions: [() => sessions.open(), () => sessions.expectBooking()],
      notes: [() => notes.open(), () => notes.expectVisibleCount(1)],
      "note-editor": [() => editor.open(), () => editor.expectBody("")],
      "note-edit": [() => editor.openExisting(fixture.notePath), () => editor.expectBody(fixture.noteBody)],
      "session-detail": [
        () => detail.open(fixture.sessionPath),
        () => detail.expectPrompts(),
      ],
      "admin-programmes": [() => new AdminProgrammesPage(page).open(), () => new AdminProgrammesPage(page).expectHeading()],
      "admin-programme": [() => new AdminProgrammePage(page).open(fixture.programmeId), () => new AdminProgrammePage(page).expectModules(12)],
      "admin-module": [() => new AdminModulePage(page).open(fixture.moduleId), () => new AdminModulePage(page).expectSectionCount(5)],
      "admin-section": [() => new AdminSectionPage(page).open(fixture.sectionId), () => new AdminSectionPage(page).expectPosition(1, 5)],
    };
    const result = await measurement.load(...actions[screen]);
    const api = await production.transfer();
    const completeBytes = result.transferredBytes + api.bytes;
    await info.attach("measurement", {
      body: JSON.stringify(
        { profile: BrowserMeasurement.profile, capturedAt: fixture.measuredAt, ...result, api, completeBytes },
        null,
        2,
      ),
      contentType: "application/json",
    });
    expect(result.transferredBytes).toBeGreaterThan(0);
    expect(completeBytes).toBeLessThanOrEqual(300_000);
    if (screen === "curriculum") {
      expect(result.interactiveMs).toBeLessThanOrEqual(2500);
      await curriculum.resume();
      await module.expectSection(1);
    }
  });
}
