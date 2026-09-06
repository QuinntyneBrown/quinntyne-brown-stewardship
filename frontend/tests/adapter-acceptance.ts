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
import { SignInService } from "../projects/api/src/sign-in.service";
import { CohortService } from "../projects/api/src/cohort.service";
import { ServiceError } from "../projects/api/src/service-error";

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
  console.log(
    "PASS: production adapters preserve credential, CSRF, session, enrollment, sign-out, and error contracts.",
  );
} finally {
  injector.destroy();
}
