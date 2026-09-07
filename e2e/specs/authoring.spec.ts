import { expect, Page, test } from '@playwright/test';
import { MockBridge } from '../page-objects/mock-bridge';
import { CurriculumPage } from '../page-objects/curriculum-page';
import { SignInPage } from '../page-objects/sign-in-page';
import { AdminProgrammesPage } from '../page-objects/admin-programmes-page';
import { AdminProgrammePage } from '../page-objects/admin-programme-page';
import { AdminModulePage } from '../page-objects/admin-module-page';
import { AdminSectionPage } from '../page-objects/admin-section-page';
import { AdminScreenPage } from '../page-objects/admin-screen-page';
import { ModulePage } from '../page-objects/module-page';

test.beforeEach(async ({ page }) => {
  await page.route('**/*', route => {
    if (['xhr', 'fetch'].includes(route.request().resourceType())) throw new Error('Mocked authoring journeys must not reach HTTP adapters.');
    return route.continue();
  });
});

// Traces to: L2-041 AC1–2, L2-042 AC4, L2-030 AC3. Given a session that reports administrator
// authority and one that does not, when the navigation renders, then only the first is offered Authoring.
test('the navigation offers Authoring only to an administrator', async ({ page, context }) => {
  await new MockBridge(page).administrator();
  const curriculum = new CurriculumPage(page);
  await curriculum.open(); await curriculum.expectDestinations(['Curriculum', 'Sessions', 'Notes', 'Authoring']);
  const other = await context.newPage();
  await new MockBridge(other).enrolled();
  const participant = new CurriculumPage(other);
  await participant.open(); await participant.expectPath(); await participant.expectDestinations(['Curriculum', 'Sessions', 'Notes'], ['Authoring']);
});

// Traces to: L2-042 AC2, L2-043 AC2. Given a signed-in participant without administrator authority,
// when they request an authoring screen by URL, then they are returned to the curriculum with an explanation.
test('a participant opening an authoring URL is returned to the curriculum with an explanation', async ({ page }) => {
  await new MockBridge(page).enrolled();
  const programmes = new AdminProgrammesPage(page); const curriculum = new CurriculumPage(page);
  await programmes.open();
  await curriculum.expectError('Curriculum authoring is not open to your account. You have been returned to your curriculum.');
  await curriculum.expectPath(); await programmes.expectAbsent();
});

// Traces to: L2-042 AC3, AC5–AC6. Given an unauthenticated visitor holding an authoring link, when they
// reach sign-in, then the route is named and nothing in it resolved, and signing in takes them to it.
test('an authoring deep link survives sign-in and names only its route', async ({ page }) => {
  await new MockBridge(page).administratorSignedOut();
  const signIn = new SignInPage(page); const programmes = new AdminProgrammesPage(page);
  await programmes.open();
  await signIn.expectVisible(); await signIn.expectHolding('/admin/programmes'); await signIn.expectNoAuthoredContent();
  await signIn.signIn();
  await programmes.expectHeading(); await programmes.expectTrail('Programmes');
});

// Traces to: L2-042 AC1, L2-052 AC4. Given an administrator, when they open the programme index,
// then every programme is listed with its publication state as text.
test('an administrator reads the programme index with each state as text', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programmes = new AdminProgrammesPage(page);
  await programmes.open(); await programmes.expectHeading();
  await programmes.expectProgramme('Stewardship — core', 'Published');
  await programmes.expectProgramme('Redemptive practice intensive', 'Published');
  await programmes.expectProgramme('Foundations for founders', 'Draft');
});

// Traces to: L2-044 AC3. Given no programmes, when the index opens, then an explicit empty state stands in for the list.
test('an empty programme index states its absence', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator(); await mocks.noProgrammes();
  const programmes = new AdminProgrammesPage(page);
  await programmes.open(); await programmes.expectEmptyState();
});

// Traces to: L2-059 AC1, L2-029, L2-031–L2-033. Given the programme index, when it renders from XS to XL,
// then it fits the viewport, its controls meet the target size, and it is accessible.
test('the programme index fits XS through XL and exposes accessible controls', async ({ page }, info) => {
  await new MockBridge(page).administrator();
  const programmes = new AdminProgrammesPage(page);
  await programmes.open(); await programmes.expectHeading();
  for (const width of [320, 576, 768, 992, 1440]) { await page.setViewportSize({ width, height: 900 }); await programmes.expectFitsViewport(); await programmes.expectTargets(); }
  await programmes.expectAccessible(); await programmes.capture(info.outputPath('admin-programmes.png'));
});

// Traces to: L2-044 AC1–AC2, L2-051 AC1, L2-060 AC3. Given the programme index, when a programme is created with a
// key another programme holds, then nothing is stored, the refusal names the key and is announced, focus moves to the
// field, and the typed title stays; a free key creates the programme in draft and opens it.
test('create a programme, refuse a duplicate key, and keep the typed title', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programmes = new AdminProgrammesPage(page); const programme = new AdminProgrammePage(page);
  await programmes.open(); await programmes.expectHeading();
  await programmes.create('stewardship-core', 'Stewardship — core, revised');
  await programmes.expectKeyRefused('The key stewardship-core is already used by another programme. Choose a different key.');
  await programmes.expectTypedTitle('Stewardship — core, revised'); await programmes.expectProgrammeCount(3);
  await programmes.create('stewardship-alumni', 'Stewardship — alumni');
  await programme.expectHeading('Stewardship — alumni'); await programme.expectTrail('Programmes', 'Stewardship — alumni');
  await programme.expectMeta('no cohort follows it yet'); await programme.expectNoModules();
  await programmes.open(); await programmes.expectProgramme('Stewardship — alumni', 'Draft'); await programmes.expectProgrammeCount(4);
});

// Traces to: L2-044 AC4, AC7–AC9, L2-063 AC1. Given a programme no cohort follows, when its title and key are
// revised, then each save is confirmed and survives a reload, a taken key is refused by name, and an unsaved
// title asks before the screen is left; a followed programme offers neither a key change nor removal.
test('rename and re-key a programme no cohort follows; a followed programme refuses both', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programme = new AdminProgrammePage(page);
  await programme.open('curriculum-3'); await programme.expectHeading('Foundations for founders');
  await programme.renameTo('Foundations for founders, revised'); await programme.expectStatus('Title saved.');
  await programme.rekeyTo('foundations-revised'); await programme.expectStatus('Key saved.');
  await page.reload(); await programme.expectHeading('Foundations for founders, revised'); await programme.expectMeta('foundations-revised · created');
  await programme.rekeyTo('stewardship-core'); await programme.expectKeyError('The key stewardship-core is already used by another programme. Choose a different key.');
  await programme.typeTitle('An unsaved title'); await programme.leaveKeepingChanges(); await programme.expectTitleValue('An unsaved title');
  await programme.open('curriculum-1'); await programme.expectHeading('Stewardship — core');
  await programme.expectKeyLocked(); await programme.expectRemovalBlocked('2 cohorts follow this programme. It cannot be removed while they do.');
});

// Traces to: L2-044 AC5–AC6, L2-060 AC5. Given a programme no cohort follows, when removal is chosen, then a dialog
// states the consequence and holds focus, and confirming removes the programme from the index.
test('remove a programme no cohort follows after confirming, with focus returned', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programmes = new AdminProgrammesPage(page); const programme = new AdminProgrammePage(page);
  await programme.open('curriculum-3'); await programme.expectHeading('Foundations for founders'); await programme.expectModules(4);
  await programme.removeAfterConfirming();
  await programmes.expectHeading(); await programmes.expectStatus('Programme removed.');
  await programmes.expectAbsentProgramme('Foundations for founders'); await programmes.expectProgrammeCount(2);
});

// Traces to: L2-054 AC1–AC2, AC4, L2-060 AC4. Given a programme whose third module has no sections, when
// publication is attempted, then the panel already states the reason and the attempt is refused naming the module.
test('publication is refused while a module has no sections and names it', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programme = new AdminProgrammePage(page);
  await programme.open('curriculum-3'); await programme.expectHeading('Foundations for founders');
  await programme.expectPublication('Draft', 'Never published');
  await programme.expectReadiness('1 module carries no section: 03 Choose enough. Every module needs at least one before this programme can be published.');
  await programme.publish();
  await programme.expectPublicationRefused('1 module carries no section: 03 Choose enough. Every module needs at least one before this programme can be published.');
  await programme.expectPublication('Draft', 'Never published');
});

// Traces to: L2-054 AC3, L2-053 AC1, L2-052 AC4. Given a complete draft programme a cohort follows, when it is
// published, then the panel confirms it, the state reads Published with its time, and the index agrees.
test('a complete draft programme publishes and the index shows it published', async ({ page }) => {
  await new MockBridge(page).administrator({ published: false, moduleCount: 8, sectionCount: 4 });
  const programme = new AdminProgrammePage(page); const programmes = new AdminProgrammesPage(page);
  await programme.open('curriculum-1'); await programme.expectHeading('Stewardship — core');
  await programme.expectPublication('Draft', 'Never published'); await programme.expectReadiness('Ready to publish. Every module carries at least one section.');
  await programme.expectShape(0, 0, 0);
  await programme.publish();
  await programme.expectStatus('Programme published. 8 modules now reach every cohort following it.');
  await programme.expectPublication('Published', 'Last published'); await programme.expectShape(8, 32, 8);
  await programmes.open(); await programmes.expectProgramme('Stewardship — core', 'Published');
});

// Traces to: L2-055 AC1–AC2, L2-052 AC2, L2-058 AC1. Given a published programme with a module added since, when
// its panel and the index are read, then the pending module is counted, the cohorts it reaches are listed with their
// own figures, and a participant still sees the published count.
test('the publication panel reports the cohorts it reaches and the modules awaiting publication', async ({ page, context }) => {
  await new MockBridge(page).administrator({ pendingModules: 1 });
  const programme = new AdminProgrammePage(page); const programmes = new AdminProgrammesPage(page);
  await programme.open('curriculum-1'); await programme.expectHeading('Stewardship — core');
  await programme.expectModules(13); await programme.expectPublication('Published', 'Last published');
  await programme.expectReadiness('1 module has been added since the last publication and is not visible to any participant.');
  await programme.expectReadiness('2 active cohorts follow this programme. Publishing changes what they read on their next request.');
  await programme.expectShape(12, 60, 12);
  await programme.expectCohortsFollowing('12 weeks · a session every 2 weeks · 6 sessions', '12 weeks · a session every 2 weeks · 6 sessions');
  await programmes.open(); await programmes.expectRowDetail('Stewardship — core', '13 modules · 12 published · 2 cohorts follow it');
  const other = await context.newPage(); await new MockBridge(other).enrolled({ pendingModules: 1 });
  const curriculum = new CurriculumPage(other); await curriculum.open(); await curriculum.expectPath(12); await curriculum.expectProgress(0, 12);
});

// Traces to: L2-045 AC1–AC2, L2-046 AC1–AC4, L2-052 AC2. Given a programme, when a module is added, then it takes the last
// position in draft; when a published module is revised with an effort estimate and three steps, then the same account,
// reading as a participant, sees every value as authored, and none of a practice block once the steps are removed.
test('add a module, revise it, and read the revision as a participant', async ({ page }) => {
  await new MockBridge(page).administratorEnrolled();
  const programme = new AdminProgrammePage(page); const module = new AdminModulePage(page); const reader = new ModulePage(page);
  await programme.open('curriculum-1'); await programme.expectModules(12);
  await programme.addModule('Handing the practice on', 'Leave the practice in good hands.');
  await module.expectHeading('Handing the practice on'); await module.expectPosition(13, 13); await module.expectTrail('Programmes', 'Stewardship — core', '13 Handing the practice on');
  await module.open('module-1'); await module.expectHeading('Begin with stewardship'); await module.expectPosition(1, 13);
  await module.fill('Title', 'Begin again'); await module.fill('Effort estimate', '45 minutes');
  await module.removeStep(3, 3); await module.removeStep(2, 2); await module.removeStep(1, 1);
  await module.addStep('Audit one default.'); await module.addStep('Compare two alternatives.'); await module.addStep('Propose a reversible change.');
  await module.moveStepUp(3, 3); await module.expectSteps('Audit one default.', 'Propose a reversible change.', 'Compare two alternatives.');
  await module.expectStepCannotMove(1, 3, 'up'); await module.expectStepCannotMove(3, 3, 'down');
  await module.save(); await module.expectSaved();
  await reader.open(1); await reader.expectHeading('Begin again'); await reader.expectPractice('45 minutes', 'Audit one default.', 'Propose a reversible change.', 'Compare two alternatives.');
  await module.open('module-1'); await module.removeStep(3, 3); await module.removeStep(2, 2); await module.removeStep(1, 1); await module.save(); await module.expectSaved();
  await reader.open(1); await reader.expectNoPractice();
});

// Traces to: L2-051 AC1–AC2, AC8–AC9, L2-060 AC3. Given a title left empty and a summary over its maximum, when the
// module is saved, then every refusal names its field with the maximum and overage, is announced, and focus moves
// to the first refused field; nothing is stored.
test('validation names every field with its maximum and overage, is announced, and moves focus', async ({ page }) => {
  await new MockBridge(page).administrator();
  const module = new AdminModulePage(page);
  await module.open('module-3'); await module.expectHeading('The cost of what we build');
  await module.fill('Title', ''); await module.fill('Summary', 'S'.repeat(442)); await module.expectCount('442 / 400');
  await module.save();
  await module.expectRefused(2, 'Title — A title is required.', 'Summary — A summary may be at most 400 characters. Shorten it by 42.');
  await module.expectFieldError('Title', 'A title is required.'); await module.expectFieldError('Summary', 'A summary may be at most 400 characters. Shorten it by 42.');
  await module.expectFocused('Title'); await module.expectTrail('Not saved');
  await page.reload(); await module.expectHeading('The cost of what we build'); await module.expectValue('Title', 'The cost of what we build');
});

// Traces to: L2-063 AC1, AC3–AC5. Given unsaved changes, when the screen is left, then a warning names the changed
// fields and staying keeps every change; discarding restores the saved values and says nothing is unsaved; a saved
// screen is left without a warning.
test('unsaved changes warn on navigation, survive choosing to stay, and discard restores the saved values', async ({ page }) => {
  await new MockBridge(page).administrator();
  const module = new AdminModulePage(page);
  await module.open('module-3'); await module.expectHeading('The cost of what we build');
  await module.fill('Title', 'The cost of what we build, and who pays it'); await module.fill('Summary', 'Every technology sends someone a bill.');
  await module.expectUnsaved(2); await module.expectTrail('Unsaved changes');
  await module.expectLeaveWarning('The title and the summary of this module have been changed and not saved. If you leave now those changes are lost.');
  await module.expectValue('Title', 'The cost of what we build, and who pays it'); await module.expectValue('Summary', 'Every technology sends someone a bill.');
  await module.discard(); await module.expectStatus('Changes discarded. Nothing is unsaved.');
  await module.expectValue('Title', 'The cost of what we build'); await expect(page.getByText('No unsaved changes.')).toBeVisible();
  await module.fill('Title', 'Left behind'); await module.leave('Discard changes'); await module.expectLeft();
  await module.open('module-3'); await module.expectValue('Title', 'The cost of what we build');
  await module.leaveViaTrail(); await module.expectLeft();
});

// Traces to: L2-063 AC2. Given unsaved changes, when the tab is reloaded, then the browser warns first and staying
// keeps the typed content.
test('a reload with unsaved changes is warned by the browser', async ({ page }) => {
  await new MockBridge(page).administrator();
  const module = new AdminModulePage(page);
  await module.open('module-3'); await module.expectHeading('The cost of what we build');
  await module.fill('Summary', 'Typed and not yet saved.');
  await module.reloadAndStay();
  await module.expectValue('Summary', 'Typed and not yet saved.');
});

// Traces to: L2-065 AC1–AC4. Given a module revised elsewhere since it was opened, when it is saved, then the save is
// refused with the current content offered for comparison, and reloading with the typed values kept lets the next
// save succeed.
test('a stale save explains the change and offers the current content', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator(); await mocks.staleRevision();
  const module = new AdminModulePage(page);
  await module.open('module-3'); await module.expectHeading('The cost of what we build');
  await module.fill('Summary', 'My revision of the summary.'); await module.save();
  await module.expectConflict(); await module.expectCurrentContent('The cost of what we build (revised elsewhere)');
  await module.expectValue('Summary', 'My revision of the summary.');
  await module.reloadKeepingChanges(); await module.expectValue('Summary', 'My revision of the summary.');
  await module.save(); await module.expectSaved(); await module.expectHeading('The cost of what we build (revised elsewhere)');
});

// Traces to: L2-065 AC5. Given a session that expired while editing, when the save fails, then the typed content
// stays in the form and no redirect discards it.
test('a failed save keeps the form even when the session expired', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator();
  const module = new AdminModulePage(page);
  await module.open('module-3'); await module.expectHeading('The cost of what we build');
  await module.fill('Summary', 'Written just before the session ended.');
  await mocks.expireSession(); await module.save();
  await module.expectSessionExpiredNotice(); await module.expectValue('Summary', 'Written just before the session ended.');
});

// Traces to: L2-047 AC1–AC3, L2-050 AC3, L2-060 AC5. Given a module with an answered prompt, when prompts are authored,
// then a new one is saved and listed, the answered one cannot be removed and says why, and an unanswered one is
// removed after confirming.
test('prompts are authored and an answered prompt cannot be removed', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator(); await mocks.answeredPrompt();
  const module = new AdminModulePage(page);
  await module.open('module-1'); await module.expectHeading('Begin with stewardship');
  await module.expectPrompts('Whose experience changed your decision?');
  await module.expectPromptBlocked(1, '1 participant has answered this prompt. Revising the wording keeps that answer attached.');
  await module.addPrompt('What did you decide not to build?'); await module.save(); await module.expectSaved();
  await module.expectPrompts('Whose experience changed your decision?', 'What did you decide not to build?');
  await page.reload(); await module.expectPrompts('Whose experience changed your decision?', 'What did you decide not to build?');
  await module.removePromptAfterConfirming(2, 2); await module.expectStatus('Prompt removed.');
  await module.expectPrompts('Whose experience changed your decision?');
});

// Traces to: L2-042 AC3, AC5–AC6, L2-045 AC3. Given an administrator holding a module link who has not signed in, when
// they reach sign-in, then only the route is named; once signed in the module opens, states the week it is read in,
// and a module nothing depends on is removed after confirming, the modules after it closing the gap.
test('an authoring deep link to a module survives sign-in, states its week, and removes cleanly', async ({ page }) => {
  await new MockBridge(page).administratorSignedOut();
  const signIn = new SignInPage(page); const module = new AdminModulePage(page); const programme = new AdminProgrammePage(page);
  await module.open('module-3');
  await signIn.expectVisible(); await signIn.expectHolding('/admin/modules/module-3'); await signIn.expectNoAuthoredContent();
  await signIn.signIn();
  await module.expectHeading('The cost of what we build'); await module.expectPosition(3, 12);
  await module.removeAfterConfirming();
  await programme.expectHeading('Stewardship — core'); await programme.expectStatus('Module removed.'); await programme.expectModules(11);
  await programme.edit('Repair as a discipline'); await module.expectHeading('Repair as a discipline'); await module.expectPosition(3, 11);
});

// Traces to: L2-048 AC1–AC2, L2-051 AC3, L2-066 AC1. Given a module, when a section is added and then revised with
// markup and two paragraphs, then the same account, reading as a participant, finds it last, split into paragraphs,
// with the markup shown as the characters typed.
test('add and revise a section, then read it as a participant with markup shown literally', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administratorEnrolled(); await mocks.completedSections('section-1-1', 'section-1-2', 'section-1-3', 'section-1-4', 'section-1-5');
  const module = new AdminModulePage(page); const section = new AdminSectionPage(page); const reader = new ModulePage(page);
  await module.open('module-1'); await module.expectHeading('Begin with stewardship'); await module.expectSectionCount(5);
  await module.addSection('A sixth section', 'First paragraph.');
  await section.expectHeading('A sixth section'); await section.expectPosition(6, 6); await section.expectTrail('Programmes', 'Stewardship — core', '01 Begin with stewardship', 'Section 6');
  await section.expectSiblings('Notice the responsibility', 'Listen to experience', 'Examine the default', 'Try a repair', 'Reflect and prepare', 'A sixth section');
  await section.fill('Title', 'A sixth section, revised'); await section.fill('Reading content', '<script>literal</script>\n\nSecond paragraph.');
  await section.save(); await section.expectSaved(); await section.expectHeading('A sixth section, revised');
  // The five completed sections make the new one the participant's resume point, so it opens at once.
  await reader.open(1); await reader.expectSection(6, 6);
  await reader.expectReading('A sixth section, revised', '<script>literal</script>', 'Second paragraph.');
});

// Traces to: L2-050 AC1, AC4, L2-048 AC3, L2-060 AC5. Given a section a participant completed and one nobody has,
// when removal is attempted, then the first is refused with the reason stated on both screens, and the second is
// removed after a dialog that holds focus and returns it; the remaining sections close the gap.
test('a completed section cannot be removed and the reason is stated; an uncompleted one can, after confirmation', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator(); await mocks.completedSections('section-1-1');
  const module = new AdminModulePage(page); const section = new AdminSectionPage(page);
  await module.open('module-1'); await module.expectHeading('Begin with stewardship');
  await module.expectSectionRow(1, 'Notice the responsibility', '1 participant completed it'); await module.expectSectionBlocked(1);
  await module.expectSectionRow(5, 'Reflect and prepare', 'no completions recorded');
  await module.expectRemovalBlocked('1 completion is recorded against the sections of this module. It cannot be removed while those records stand.');
  await module.editSection('Notice the responsibility');
  await section.expectHeading('Notice the responsibility'); await section.expectRecorded('1 completion'); await section.expectRemovalBlocked();
  await module.open('module-1'); await module.removeSectionAfterConfirming('Reflect and prepare');
  await module.expectStatus('Section removed.'); await module.expectSectionCount(4); await module.expectSectionRow(4, 'Try a repair', 'no completions recorded');
  await module.editSection('Try a repair'); await section.expectPosition(4, 4); await section.removeAfterConfirming();
  await module.expectHeading('Begin with stewardship'); await module.expectStatus('Section removed.'); await module.expectSectionCount(3);
});

// Traces to: L2-051 AC6–AC7, AC9, L2-063 AC1. Given the reading field, when its text passes the maximum, then the count
// keeps counting and the save is refused naming the overage; a title left empty is refused with it.
test('the reading field counts past its maximum and the refusal states the overage', async ({ page }) => {
  await new MockBridge(page).administrator();
  const section = new AdminSectionPage(page);
  await section.open('section-1-2'); await section.expectHeading('Listen to experience'); await section.expectPosition(2, 5);
  await section.fill('Reading content', 'r'.repeat(12042)); await section.expectCount('12042 / 12000'); await section.expectReadingOver();
  await section.fill('Title', '');
  await section.save();
  await section.expectRefused(2, 'A reading may be at most 12000 characters. Shorten it by 42.');
  await expect(page.getByRole('alert').filter({ hasText: 'A title is required.' })).toBeVisible();
  await section.expectValue('Reading content', 'r'.repeat(12042));
  await page.reload(); await section.expectHeading('Listen to experience'); await section.expectValue('Title', 'Listen to experience');
});

// Traces to: L2-049 AC1, AC4, L2-060 AC1–AC2. Given the module list, when a module is moved by keyboard alone, then the
// new arrangement is stored, announced, contiguous, and still there after a reload; the ends cannot move past them.
test('move a module by keyboard and hear the new arrangement announced, and the order persists across reload', async ({ page }) => {
  await new MockBridge(page).administrator();
  const programme = new AdminProgrammePage(page);
  await programme.open('curriculum-3'); await programme.expectHeading('Foundations for founders');
  await programme.expectModuleOrder('What a founder owes', 'The first promise', 'Choose enough', 'Hand it on');
  await programme.expectModuleCannotMove('What a founder owes', 1, 4, 'up'); await programme.expectModuleCannotMove('Hand it on', 4, 4, 'down');
  await programme.moveModuleUpByKeyboard('Hand it on', 4, 4);
  await programme.expectStatus('Hand it on moved to position 3 of 4.');
  await programme.expectModuleOrder('What a founder owes', 'The first promise', 'Hand it on', 'Choose enough');
  await programme.moveModuleUp('Hand it on', 3, 4); await programme.expectStatus('Hand it on moved to position 2 of 4.');
  await page.reload(); await programme.expectModuleOrder('What a founder owes', 'Hand it on', 'The first promise', 'Choose enough');
  await programme.expectModuleCannotMove('Choose enough', 4, 4, 'down');
});

// Traces to: L2-049 AC2, L2-047 AC5, L2-059 AC3, L2-032 AC4–AC5. Given a module's sections and prompts at 320 pixels,
// when each is reordered, then the controls stay operable without horizontal scrolling and the participant reads the
// sections in the new order; prompts take their new order when the module is saved.
test('ordering controls stay operable at 320px without horizontal scrolling', async ({ page }) => {
  await new MockBridge(page).administratorEnrolled();
  const module = new AdminModulePage(page); const reader = new ModulePage(page);
  await page.setViewportSize({ width: 320, height: 900 });
  await module.open('module-1'); await module.expectHeading('Begin with stewardship');
  await module.expectFitsViewport(); await module.expectTargets();
  await module.moveSectionDown('Notice the responsibility', 1, 5); await module.expectStatus('Notice the responsibility moved to position 2 of 5.');
  await module.expectSectionOrder('Listen to experience', 'Notice the responsibility'); await module.expectFitsViewport();
  await module.addPrompt('What did you decide not to build?'); await module.movePromptUp(2, 2); await module.expectStatus('Prompt moved to position 1 of 2.');
  await module.save(); await module.expectSaved(); await module.expectPrompts('What did you decide not to build?', 'Whose experience changed your decision?');
  await reader.open(1); await reader.expectSection(1, 5); await reader.expectReading('Listen to experience', 'Stewardship begins with attention to the people affected by a technical decision. Describe an ordinary task, ask who experiences difficulty, and listen before proposing a solution. Record what would change your view.', 'Choose a small, reversible improvement. Explain its benefit and its possible cost to the person who will maintain it. Return to the people affected and check whether the improvement serves their actual needs.');
});

// Traces to: L2-049 AC3. Given a move the server refuses, when the refusal arrives, then the list returns to the stored
// order and the reason is shown.
test('an optimistic move that the server refuses reverts', async ({ page }) => {
  const mocks = new MockBridge(page); await mocks.administrator(); await mocks.refuseReorder();
  const programme = new AdminProgrammePage(page);
  await programme.open('curriculum-3'); await programme.expectHeading('Foundations for founders');
  await programme.moveModuleUp('Hand it on', 4, 4);
  await programme.expectAlert('The submitted order does not match the modules of this programme. Reload and try again.');
  await programme.expectModuleOrder('What a founder owes', 'The first promise', 'Choose enough', 'Hand it on');
  await page.reload(); await programme.expectModuleOrder('What a founder owes', 'The first promise', 'Choose enough', 'Hand it on');
});

// Traces to: L2-066 AC1–AC4. Given unsaved changes to a draft module, when it is previewed, then it reads as a participant
// would read it with paragraphs split by the same rule and markup literal, every section open, no completion or answer
// offered, nothing recorded, and returning leaves every unsaved change where it was typed.
test('preview renders a draft module as a participant reads it, offers no completion, records nothing, and returns unsaved changes to their fields', async ({ page }) => {
  await new MockBridge(page).administratorEnrolled();
  const module = new AdminModulePage(page); const reader = new ModulePage(page); const curriculum = new CurriculumPage(page);
  await module.open('c3-module-1'); await module.expectHeading('What a founder owes');
  await module.fill('Title', 'What a founder owes <script>literal</script>'); await module.fill('Effort estimate', '90 minutes');
  await module.addStep('Name the promise you made.');
  await module.openPreview();
  await reader.expectHeading('What a founder owes <script>literal</script>'); await reader.expectSection(1, 3);
  await reader.expectReading('Notice the responsibility', 'Stewardship begins with attention to the people affected by a technical decision. Describe an ordinary task, ask who experiences difficulty, and listen before proposing a solution. Record what would change your view.', 'Choose a small, reversible improvement. Explain its benefit and its possible cost to the person who will maintain it. Return to the people affected and check whether the improvement serves their actual needs.');
  await reader.expectPractice('90 minutes', 'Observe a real task with permission.', 'Identify a cost carried by someone else.', 'Try a small change and record what happened.', 'Name the promise you made.');
  await reader.expectEverySectionSelectable(); await reader.choose(3); await reader.expectSection(3, 3); await reader.expectNoCompletionAction();
  await module.backToEditor();
  await module.expectValue('Title', 'What a founder owes <script>literal</script>'); await module.expectValue('Effort estimate', '90 minutes'); await module.expectSteps('Observe a real task with permission.', 'Identify a cost carried by someone else.', 'Try a small change and record what happened.', 'Name the promise you made.');
  await module.expectUnsaved(3);
  await module.discard(); await module.expectValue('Title', 'What a founder owes');
  await curriculum.open(); await curriculum.expectPath(12); await curriculum.expectProgress(0, 12);
});

// Traces to: L2-059 AC1–AC5, L2-060 AC1, AC4, L2-029, L2-031–L2-034. Given each administration screen, when it renders
// from XS to XL, then it fits the viewport, every control meets its target size, it is accessible, the actions at XS are
// the actions at XL, the trail condenses to one link at XS, and every state is read as text.
for (const [name, open, parent] of [
  ['admin-programme', async (page: Page) => { const programme = new AdminProgrammePage(page); await programme.open('curriculum-1'); await programme.expectHeading('Stewardship — core'); return programme as AdminScreenPage; }, 'Programmes'],
  ['admin-module', async (page: Page) => { const module = new AdminModulePage(page); await module.open('module-1'); await module.expectHeading('Begin with stewardship'); return module as AdminScreenPage; }, 'Stewardship — core'],
  ['admin-section', async (page: Page) => { const section = new AdminSectionPage(page); await section.open('section-1-2'); await section.expectHeading('Listen to experience'); return section as AdminScreenPage; }, 'module 01'],
] as const) {
  test(`the ${name.replace('admin-', '')} editor fits XS through XL and exposes accessible controls`, async ({ page }, info) => {
    const mocks = new MockBridge(page); await mocks.administrator(); await mocks.completedSections('section-1-1');
    await page.setViewportSize({ width: 1440, height: 900 });
    const screen = await open(page);
    await screen.expectFullTrail(); if (name !== 'admin-section') await screen.expectStateAsText('Published');
    const atLarge = await screen.actionNames();
    for (const width of [320, 576, 768, 992, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await screen.expectFitsViewport(); await screen.expectTargets();
      if (width === 320) { await screen.expectCondensedTrail(parent); expect(await screen.actionNames()).toEqual(atLarge); }
    }
    await screen.expectAccessible(); await screen.capture(info.outputPath(`${name}.png`));
  });
}
