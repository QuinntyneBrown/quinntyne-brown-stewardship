import { test } from '@playwright/test';
import { MockBridge } from '../page-objects/mock-bridge';
import { CurriculumPage } from '../page-objects/curriculum-page';
import { ModulePage } from '../page-objects/module-page';
import { SessionsPage } from '../page-objects/sessions-page';
import { NotesPage } from '../page-objects/notes-page';
import { NoteEditorPage } from '../page-objects/note-editor-page';
import { SessionDetailPage } from '../page-objects/session-detail-page';

test.beforeEach(async ({ page }) => {
  await page.route('**/*', route => {
    if (['xhr', 'fetch'].includes(route.request().resourceType())) throw new Error('Mocked programme journeys must not reach HTTP adapters.');
    return route.continue();
  });
});

// Traces to: L2-008–016. Given enrollment, when learning through five sections,
// then resume survives reload and completing the module advances the curriculum.
test('learn a complete module and resume persisted section progress', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const curriculum = new CurriculumPage(page);
  const module = new ModulePage(page);
  await curriculum.open();
  await curriculum.expectPath();
  await curriculum.expectProgress(0);
  await curriculum.resume();
  await module.expectSection(1);
  await module.expectSameAssignmentAcrossViewports();
  await module.completeSection();
  await module.expectSection(2);
  await module.reload();
  await module.expectSection(2);
  for (let section = 2; section <= 5; section++) {
    await module.expectSection(section);
    await module.completeSection();
  }
  await curriculum.expectProgress(1);
});

// Traces to: L2-014 AC1–4. Given five sections, when none, two or all five
// are complete, then the labelled progress indicator fills 0%, 40% or 100%.
test('section progress pairs the current position with recorded completion', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const module = new ModulePage(page);
  await module.open(); await module.expectProgress(0, 1);
  for (let completed = 1; completed <= 4; completed++) {
    await module.completeSection(); await module.expectProgress(completed, completed + 1);
  }
  await module.completeSection(); await new CurriculumPage(page).expectProgress(1);
  await module.open(1); await module.expectProgress(5, 5);
});

// Traces to: L2-017–024. Given open availability, when a booking is made,
// changed and cancelled, then the next-session and booking controls follow it.
test('book, reschedule and cancel a mentor conversation', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const sessions = new SessionsPage(page);
  await sessions.open(); await sessions.selectFirstOpen(); await sessions.book();
  await sessions.expectBooking(); await sessions.reschedule(); await sessions.expectBooking();
  await sessions.cancel(); await sessions.expectCanBook();
});

// Traces to: L2-024. Given a lost slot race, when booking fails, then the
// rejection is explained and the refreshed slot is taken.
test('a contested slot refreshes without creating a booking', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.enrolled(); await mocks.unavailableSlot();
  const sessions = new SessionsPage(page);
  await sessions.open(); await sessions.selectFirstOpen(); await sessions.book();
  await sessions.expectConflict(); await sessions.expectCanBook();
});

// Traces to: L2-025–026, L2-036. Given a draft, when saved and edited, then
// literal text persists and declined navigation preserves unsaved work.
test('write and revise a literal note without losing an unsaved draft', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const notes = new NotesPage(page); const editor = new NoteEditorPage(page);
  await notes.open(); await notes.add();
  await editor.write('<script>literal reflection</script>'); await editor.save();
  await notes.expectNote('<script>literal reflection</script>'); await notes.editFirst();
  await editor.write('A revised reflection'); await editor.keepUnsavedDraft();
  await editor.expectBody('A revised reflection'); await editor.save();
  await notes.expectNote('A revised reflection');
});

// Traces to: L2-027. Preparation answers remain attached to their original prompt.
test('a preparation answer returns beneath the session prompt', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const sessions = new SessionsPage(page); const detail = new SessionDetailPage(page); const editor = new NoteEditorPage(page);
  await sessions.open(); await sessions.selectFirstOpen(); await sessions.book(); await sessions.prepare();
  await detail.expectPrompts(); await detail.answer(); await editor.write('The person carrying the cost changed my view.'); await editor.save();
  await sessions.open(); await sessions.prepare(); await detail.expectAnswer('The person carrying the cost changed my view.');
});

// Traces to: L2-029–034. Each complete screen remains accessible and reachable.
test('all programme screens fit XS through XL and expose accessible controls', async ({ page }, info) => {
  test.setTimeout(120000);
  await new MockBridge(page).enrolled();
  const curriculum = new CurriculumPage(page); const module = new ModulePage(page); const sessions = new SessionsPage(page);
  const notes = new NotesPage(page); const editor = new NoteEditorPage(page); const detail = new SessionDetailPage(page);
  await curriculum.open(); await curriculum.expectPath();
  for (const [name, screen, open] of [
    ['curriculum', curriculum, async () => curriculum.open()],
    ['module', module, async () => { await curriculum.open(); await curriculum.resume(); await module.expectSection(1); }],
    ['sessions', sessions, async () => { await sessions.open(); await sessions.expectCanBook(); }],
    ['notes', notes, async () => notes.open()],
    ['note-editor', editor, async () => { await notes.open(); await notes.add(); await editor.expectBody(''); }],
    ['session-detail', detail, async () => { await sessions.open(); await sessions.selectFirstOpen(); await sessions.book(); await sessions.prepare(); await detail.expectPrompts(); }],
  ] as const) {
    await open();
    for (const width of [320, 576, 768, 992, 1440]) { await page.setViewportSize({ width, height: 900 }); await screen.expectFitsViewport(); await screen.expectTargets(); }
    await screen.expectAccessible(); await screen.capture(info.outputPath(`${name}.png`));
  }
});

// Traces to: L2-029, L2-040. A programme read failure is explained and retryable.
test('curriculum loading errors recover through an explicit retry', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.enrolled(); await mocks.failingProgramme();
  const curriculum = new CurriculumPage(page); await curriculum.open(); await curriculum.expectError('temporarily unavailable');
  await mocks.allowProgramme(); await curriculum.retry(); await curriculum.expectPath();
});

// Traces to: L2-031 AC1, L2-034 AC1. Given sections not yet started,
// when their disabled navigation labels render, then their state remains readable
// at the required 4.5:1 text contrast (including the control's opacity).
test('not-started section labels retain readable contrast', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const curriculum = new CurriculumPage(page);
  const module = new ModulePage(page);
  await curriculum.open(); await curriculum.resume(); await module.expectSection(1);
  await module.expectSectionStateContrast();
});

// Traces to: L2-018, L2-033 AC1–4. Given a keyboard-only participant,
// when booking and opening then dismissing cancellation, then controls work,
// focus enters the dialog, moves between its actions, and returns to its trigger.
test('book and keep a session using only the keyboard', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const sessions = new SessionsPage(page);
  await sessions.open(); await sessions.expectCanBook();
  await sessions.bookByKeyboard(); await sessions.expectBooking();
  await sessions.keepSessionByKeyboard(); await sessions.expectBooking();
});

// Traces to: L2-025–026, L2-039. Given several long notes, when loading more
// at the notes destination or inside a module, then every full note is reachable.
test('long notes load in pages at both their attachment and the notes destination', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.enrolled(); await mocks.longNotes();
  const notes = new NotesPage(page); const module = new ModulePage(page);
  await notes.open(); await notes.expectVisibleCount(1);
  for (let count = 2; count <= 4; count++) { await notes.more(); await notes.expectVisibleCount(count); }
  await notes.expectNoMore(); await notes.editFirst();
  await new NoteEditorPage(page).expectBody('Reflection 0: ' + 'A'.repeat(9900));
  await module.open(); await module.expectNoteCount(1); await module.moreNotes(); await module.expectNoteCount(2);
});

// Traces to: L2-025–026, L2-033, L2-040. Given an interrupted next-page request,
// when the participant retries by keyboard, then previous notes remain and focus
// reaches the first additional note without duplicates.
test('loading more notes recovers without losing the loaded notes', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.enrolled(); await mocks.longNotes();
  const notes = new NotesPage(page);
  await notes.open(); await notes.expectVisibleCount(1);
  await mocks.interruptProgramme(); await notes.more();
  await notes.expectError('temporarily unavailable'); await notes.expectVisibleCount(1);
  await mocks.allowProgramme(); await notes.moreByKeyboard();
  await notes.expectVisibleCount(2); await notes.expectNoteFocused(1);
  await notes.expectFitsViewport(); await notes.expectAccessible();
});

// Traces to: L2-026–027, L2-039. Given long session notes, when loading additional
// pages at their session attachment, then all four notes remain reachable.
test('session preparation pages through its attached long notes', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.enrolled(); await mocks.longNotes(true);
  const detail = new SessionDetailPage(page);
  await detail.open('/sessions/session-long-notes'); await detail.expectPrompts();
  await detail.expectNoteCount(1);
  for (let count = 2; count <= 4; count++) { await detail.moreNotes(); await detail.expectNoteCount(count); }
});

// Traces to: L2-056 AC1, L2-057 AC1, L2-058 AC1, AC4, L2-009 AC3, L2-023 AC1. Given an
// eight-module programme and an eight-week cohort, when the participant reads, then eight
// markers, totals against eight, the week against eight, and an allowance of four derive from it.
test('an eight-module programme and an eight-week cohort derive every displayed figure', async ({ page }) => {
  await new MockBridge(page).enrolled({ moduleCount: 8, durationWeeks: 8 });
  const curriculum = new CurriculumPage(page); const sessions = new SessionsPage(page);
  await curriculum.open(); await curriculum.expectPath(8); await curriculum.expectProgress(0, 8); await curriculum.expectRemaining(8); await curriculum.expectWeek(1, 8);
  await sessions.open(); await sessions.expectAllowance(0, 4);
});

// Traces to: L2-056 AC3. Given a programme of one module, when the curriculum renders,
// then one marker is shown and no figure implies a larger programme.
test('a one-module programme implies nothing larger', async ({ page }) => {
  await new MockBridge(page).enrolled({ moduleCount: 1 });
  const curriculum = new CurriculumPage(page);
  await curriculum.open(); await curriculum.expectPath(1); await curriculum.expectProgress(0, 1); await curriculum.expectRemaining(1);
});

// Traces to: L2-053 AC1, L2-007 AC2. Given a cohort following a draft programme, when any
// programme destination opens, then the participant is told it is not yet available and sees no path.
test('a participant on a draft programme is told it is not yet available on every destination', async ({ page }) => {
  await new MockBridge(page).enrolled({ published: false });
  const curriculum = new CurriculumPage(page);
  for (const path of ['/curriculum', '/modules/1', '/sessions', '/notes']) { await page.goto(path); await curriculum.expectNotYetAvailable(); }
});

// Traces to: L2-053 AC3. Given a module the programme does not publish, when it is requested
// by URL, then the participant returns to the curriculum with an explanation.
test('a module beyond the published programme returns to the curriculum with an explanation', async ({ page }) => {
  await new MockBridge(page).enrolled({ moduleCount: 8 });
  const curriculum = new CurriculumPage(page); const module = new ModulePage(page);
  await module.open(9); await curriculum.expectPath(8); await curriculum.expectNotice('not available');
});
