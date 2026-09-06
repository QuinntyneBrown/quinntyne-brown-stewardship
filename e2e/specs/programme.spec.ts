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
