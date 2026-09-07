import "@angular/compiler";
import assert from "node:assert/strict";
import {
  createEnvironmentInjector,
  EnvironmentInjector,
  Injector,
  runInInjectionContext,
  provideZonelessChangeDetection,
  ɵINJECTOR_SCOPE,
} from "@angular/core";
import { provideHttpClient, withNoXsrfProtection } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { CohortService, ServiceError, SignInService, CurriculumService, SessionService, NoteService, AuthoringService, ValidationError } from "@qbs/api";

// Traces to: AE-01/04/05/08/09. Exercise the production adapters and Angular HTTP
// pipeline separately from Playwright's mocked service composition.
const injector = createEnvironmentInjector(
  [
    { provide: ɵINJECTOR_SCOPE, useValue: "root" },
    provideZonelessChangeDetection(),
    provideHttpClient(withNoXsrfProtection()),
    provideHttpClientTesting(),
    SignInService,
    CohortService,
    CurriculumService, SessionService, NoteService, AuthoringService,
  ],
  Injector.create({ providers: [] }) as EnvironmentInjector,
);
const authentication = runInInjectionContext(injector, () =>
  injector.get(SignInService),
);
const cohort = runInInjectionContext(injector, () =>
  injector.get(CohortService),
);
const http = injector.get(HttpTestingController);
const tick = () => new Promise<void>((resolve) => setImmediate(resolve));

try {
  const session = authentication.session();
  http
    .expectOne({ method: "GET", url: "/authentication/session" })
    .flush({}, { status: 401, statusText: "Unauthorized" });
  assert.equal(await session, null);
  // Traces to: L2-041 AC1–2. The session adapter passes the authority the API reports straight through.
  const established = authentication.session();
  http.expectOne({ method: "GET", url: "/authentication/session" }).flush({ emailAddress: "participant@example.com", isAdministrator: true });
  assert.deepEqual(await established, { emailAddress: "participant@example.com", isAdministrator: true });

  const signIn = authentication.signIn(
    "participant@example.com",
    "a private password",
  );
  http
    .expectOne({ method: "GET", url: "/authentication/csrf" })
    .flush({ token: "before-sign-in" });
  await tick();
  const credentials = http.expectOne({
    method: "POST",
    url: "/authentication/sign-in",
  });
  assert.deepEqual(credentials.request.body, {
    emailAddress: "participant@example.com",
    password: "a private password",
  });
  assert.equal(
    credentials.request.headers.get("X-CSRF-TOKEN"),
    "before-sign-in",
  );
  credentials.flush({ statusCode: 200, message: null, retryAt: null });
  assert.equal((await signIn).statusCode, 200);

  const enrollment = cohort.getEnrollment();
  const absent = {
    isEnrolled: false,
    cohortId: null,
    mentorName: null,
    startDate: null,
    endDate: null,
    currentWeek: null,
    sessionAllowance: null,
    hasEnded: null,
    durationWeeks: null,
    sessionCadenceWeeks: null,
    isProgrammePublished: null,
  };
  http.expectOne({ method: "GET", url: "/enrollment" }).flush(absent);
  assert.deepEqual(await enrollment, absent);

  const signOut = authentication.signOut();
  http
    .expectOne({ method: "GET", url: "/authentication/csrf" })
    .flush({ token: "after-sign-in" });
  await tick();
  const revoke = http.expectOne({
    method: "POST",
    url: "/authentication/sign-out",
  });
  assert.equal(revoke.request.headers.get("X-CSRF-TOKEN"), "after-sign-in");
  revoke.flush(null, { status: 204, statusText: "No Content" });
  await signOut;

  for (const status of [401, 503]) {
    const failure = cohort.getEnrollment();
    http.expectOne("/enrollment").flush({}, { status, statusText: "Failure" });
    await assert.rejects(
      failure,
      (error) => error instanceof ServiceError && error.status === status,
    );
  }
  const rejected = authentication.signIn("unknown@example.com", "wrong");
  http.expectOne("/authentication/csrf").flush({ token: "anonymous" });
  await tick();
  http
    .expectOne("/authentication/sign-in")
    .flush(
      {
        statusCode: 401,
        message: "Email address or password is incorrect",
        retryAt: null,
      },
      { status: 401, statusText: "Unauthorized" },
    );
  assert.equal(
    (await rejected).message,
    "Email address or password is incorrect",
  );
  http.verify();
  const curriculum = injector.get(CurriculumService);
  const sessions = injector.get(SessionService);
  const notes = injector.get(NoteService);
  const authoring = injector.get(AuthoringService);
  // Traces to: L2-008–028, L2-035–037, L2-043. The production adapters preserve payloads,
  // acquire CSRF for every mutation, and retain conflict metadata for recovery.
  for (const [request, url] of [
    [() => authoring.getProgrammes(), '/administration/curricula'],
    [() => authoring.getProgramme('programme-id'), '/administration/curricula/programme-id'],
    [() => authoring.getModule('module-id'), '/administration/modules/module-id'],
    [() => authoring.getSection('section-id'), '/administration/sections/section-id'],
    [() => curriculum.getCurriculum(), '/curriculum'],
    [() => curriculum.getModule(null), '/modules/current'],
    [() => curriculum.getModule(3), '/modules/3'],
    [() => sessions.availability('2026-09-10'), '/sessions/availability?day=2026-09-10'],
    [() => sessions.history(), '/sessions/history'],
    [() => sessions.get('session-id'), '/sessions/session-id'],
    [() => sessions.preparation('session-id'), '/sessions/session-id/preparation'],
    [() => notes.list('module-id'), '/notes?moduleId=module-id'],
    [() => notes.list('module-id', undefined, 'a+b/c=', true), '/notes?moduleId=module-id&cursor=a%2Bb%2Fc%3D&generalOnly=true'],
    [() => notes.list(undefined, 'session-id', 'next'), '/notes?sessionId=session-id&cursor=next'],
    [() => notes.get('note-id'), '/notes/note-id'],
  ] as const) {
    const pending = request(); http.expectOne({ method: 'GET', url }).flush({ marker: 'unchanged' });
    assert.deepEqual(await pending, { marker: 'unchanged' });
  }
  const nextPage = notes.list(undefined, undefined, 'next');
  const nextPageBody = { notes: [{ id: 'note-2', body: 'The complete reflection.' }], nextCursor: null, attachments: [], maxLength: 10000 };
  http.expectOne('/notes?cursor=next').flush(nextPageBody);
  assert.deepEqual(await nextPage, nextPageBody);
  const body = { body: '<script>literal</script>', moduleId: 'module-id', sessionId: null };
  const sectionRevision = { title: 'Revised', reading: '<script>literal</script>', revision: 'revision-2' };
  const moduleRevision = { title: 'Begin again', summary: 'A summary.', effortEstimate: '45 minutes', practiceSteps: ['One', 'Two'], revision: 'revision-1' };
  for (const [request, method, url, expected] of [
    [() => curriculum.completeSection('section-id'), 'POST', '/sections/section-id/completion', {}],
    [() => sessions.book('slot-id'), 'POST', '/sessions', { slotId: 'slot-id' }],
    [() => sessions.reschedule('session-id', 'slot-id'), 'PUT', '/sessions/session-id/slot', { slotId: 'slot-id' }],
    [() => sessions.cancel('session-id'), 'DELETE', '/sessions/session-id', {}],
    [() => notes.save(body), 'POST', '/notes', body],
    [() => authoring.createProgramme('eight', 'Eight weeks'), 'POST', '/administration/curricula', { key: 'eight', title: 'Eight weeks' }],
    [() => authoring.renameProgramme('programme-id', 'Renamed'), 'PUT', '/administration/curricula/programme-id', { title: 'Renamed' }],
    [() => authoring.changeKey('programme-id', 'nine'), 'PUT', '/administration/curricula/programme-id/key', { key: 'nine' }],
    [() => authoring.removeProgramme('programme-id'), 'DELETE', '/administration/curricula/programme-id', {}],
    [() => authoring.publish('programme-id'), 'POST', '/administration/curricula/programme-id/publication', {}],
    [() => authoring.addModule('programme-id', 'Module', 'Summary'), 'POST', '/administration/curricula/programme-id/modules', { title: 'Module', summary: 'Summary' }],
    [() => authoring.reviseModule('module-id', moduleRevision), 'PUT', '/administration/modules/module-id', moduleRevision],
    [() => authoring.removeModule('module-id'), 'DELETE', '/administration/modules/module-id', {}],
    [() => authoring.addPrompt('module-id', 'Why?'), 'POST', '/administration/modules/module-id/prompts', { text: 'Why?' }],
    [() => authoring.revisePrompt('prompt-id', 'Why not?'), 'PUT', '/administration/prompts/prompt-id', { text: 'Why not?' }],
    [() => authoring.removePrompt('prompt-id'), 'DELETE', '/administration/prompts/prompt-id', {}],
    [() => authoring.addSection('module-id', 'Section', 'Reading.'), 'POST', '/administration/modules/module-id/sections', { title: 'Section', reading: 'Reading.' }],
    [() => authoring.reviseSection('section-id', sectionRevision), 'PUT', '/administration/sections/section-id', sectionRevision],
    [() => authoring.removeSection('section-id'), 'DELETE', '/administration/sections/section-id', {}],
    [() => authoring.reorderModules('programme-id', ['b', 'a']), 'PUT', '/administration/curricula/programme-id/modules/order', { order: ['b', 'a'] }],
    [() => authoring.reorderSections('module-id', ['b', 'a']), 'PUT', '/administration/modules/module-id/sections/order', { order: ['b', 'a'] }],
    [() => authoring.reorderPrompts('module-id', ['b', 'a']), 'PUT', '/administration/modules/module-id/prompts/order', { order: ['b', 'a'] }],
  ] as const) {
    const pending = request(); http.expectOne('/authentication/csrf').flush({ token: 'programme-token' }); await tick();
    const sent = http.expectOne({ method, url }); assert.deepEqual(sent.request.body, expected); assert.equal(sent.request.headers.get('X-CSRF-TOKEN'), 'programme-token');
    sent.flush(null); await pending;
  }
  const conflict = sessions.book('contested'); http.expectOne('/authentication/csrf').flush({ token: 'programme-token' }); await tick();
  http.expectOne('/sessions').flush({ title: 'That slot is no longer available.', correlationId: 'request-42' }, { status: 409, statusText: 'Conflict' });
  await assert.rejects(conflict, e => e instanceof ServiceError && e.status === 409 && e.message.includes('no longer available') && e.correlationId === 'request-42');
  // Traces to: L2-043 AC2. A refused authoring call keeps its status and reason.
  const refused = authoring.getProgrammes();
  http.expectOne('/administration/curricula').flush({ title: 'Curriculum authoring is not open to your account.' }, { status: 403, statusText: 'Forbidden' });
  await assert.rejects(refused, e => e instanceof ServiceError && e.status === 403 && e.message.includes('not open to your account'));
  // Traces to: L2-051 AC8–AC9. A refused write names every failing field; the adapter keeps each message by field.
  const invalid = authoring.createProgramme('', ''); http.expectOne('/authentication/csrf').flush({ token: 'programme-token' }); await tick();
  http.expectOne('/administration/curricula').flush({ title: 'One or more validation errors occurred.', errors: { key: ['A key is required.'], title: ['A title is required.'] } }, { status: 400, statusText: 'Bad Request' });
  await assert.rejects(invalid, e => e instanceof ValidationError && e.errorFor('title') === 'A title is required.' && e.errorFor('key') === 'A key is required.' && e.errorFor('summary') === '');
  http.verify();
  console.log(
    "PASS: production adapters preserve credential, CSRF, session, enrollment, sign-out, and error contracts.",
  );
} finally {
  injector.destroy();
}
