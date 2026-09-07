# Testing guide

Stewardship is built with acceptance test-driven development. Every suite here
proves behaviour; none of them assert the shape of the codebase. Structure,
layout, and naming are the job of the compiler, the formatter, and review.

## Running everything

```powershell
. ./scripts/use-local-sql.ps1
dotnet test backend/QuinntyneBrownStewardship.sln
npm --prefix e2e run install:browsers
npm run test:e2e
npm run test:adapters
npm --prefix design-system run build
npm --prefix design-system test
```

Performance checks run separately, so that competing work does not distort the
normal-load measurements:

```powershell
npm run test:performance:api
npm run test:performance:web
```

Run the API performance command **before** the browser one. It refreshes the
captured response fixture that the browser check replays.

## API acceptance tests

`backend/tests/QuinntyneBrownStewardship.Api.Tests`

Integration tests against the real API. They use real persistence, real password
hashing, real cookies, and real HTTP endpoints. A controlled clock exercises
expiry and throttling without waiting for wall-clock time to pass.

Each run creates and deletes a uniquely named SQL Server database. The login
behind `STEWARDSHIP_TEST_SQL` must be allowed to create test databases. **No
existing application database is cleared.**

## End-to-end tests

`e2e/`

A self-contained Playwright package: its own `package.json`, lock file,
`playwright.config.ts`, and `tsconfig.json` sit beside `page-objects/` and
`specs/`. `npm run test:e2e` delegates to it. It uses **port 4317**.

The suite binds service tokens to mocks in its own dedicated Angular build and
refuses authentication and enrollment HTTP calls outright, so a test can never
reach a real implementation by accident.

Tests follow the Page Object Model. One page object per screen owns the selectors
and the interactions; tests state intent. **A selector never appears in a test.**

`specs/authoring.spec.ts` covers the administrator screens with the same
discipline. Its target-size check exempts the stacked order-control pair: each
half is at least 24 px tall, the pair together is at least 44 × 44, and each half
carries its own accessible name.

HTML reports and screen captures are retained under `e2e/playwright-report/` and
`e2e/test-results/`. Traces are retained on failure.

## Adapter tests

`npm run test:adapters`

The end-to-end suite deliberately never exercises the production adapters, so
those are checked separately. This suite runs the real adapters against Angular's
HTTP testing backend and verifies the requests they issue, their CSRF headers,
their response handling, and their errors.

## Design system tests

`design-system/`

A separate Playwright package, run against the built catalogue on **port 4318**.
The design system carries no runtime dependency on the application and does not
load it.

## Performance

Both harnesses are repeatable lab simulations with explicit budgets, not
production telemetry.

### API

`npm run test:performance:api`

Sends 100 measured requests per operation from five authenticated participants
against a temporary SQL Server database, using the Release ASP.NET HTTP test host.
It measures real section completion, note creation and revision, booking,
rescheduling, cancellation, sign-in, and sign-out, alongside every read endpoint,
enrollment, and health: **21 operations**, plus four compressed reads with
full-length Unicode notes, plus eight authoring operations from five
administrators — the programme index, a programme, a module, and a section with
a 12,000-character reading, a section and a module revision, a reorder, and a
publication — for **33 measured scenarios**.

Successful authentication measurements clear the isolated fixture's attempt
history between warm-up and measured requests. Rate limits remain enabled
throughout, and concurrent refusals are verified separately.

Results are written to `.local/api-performance.json`.

For a focused diagnostic, append `--sign-in-only` directly to the performance
project's `dotnet run` arguments. The complete gate still uses all 33 scenarios.

The same command writes `.local/production-responses.json` from a separate,
populated participant: twenty maximum-length Unicode module notes, twenty Unicode
session notes, three maximum-length Unicode preparation answers, five past
sessions, and one future session, together with the administrator's session and
the four administration reads of the twelve-module programme. It records real
response DTOs, server time, and sizes as compressed by the production middleware,
with application headers and an additional 1 KB transport-header reserve.

### Browser

`npm run test:performance:web`

Builds optimized Angular assets with service-token doubles that return the
captured DTOs. Production adapter code stays in the measured bundle; the doubles
override those tokens before services are instantiated. Brotli assets are served
on **port 4319** at quality 4, matching production's `CompressionLevel.Optimal`.

A cold-cache Chromium profile runs with 4× CPU slowdown, 150 ms network latency,
and 4 Mbps down / 1 Mbps up. Each service double waits for the captured server
time plus the simulated latency and response transfer.

For each of **thirteen screens**, the 300 KB gate adds every captured API response
actually used to the browser asset transfers, including fonts and headers. The
check also requires usable curriculum controls within **2.5 seconds**.

JSON results, resource timings, and API calls are written to
`.local/web-performance.json`.

### Deliberate performance decisions

- **Critical CSS inlining is disabled.** Its deferred stylesheet caused duplicate
  CSS and body-font downloads under the application's `no-store` cache policy.
- **Newsreader uses its default optical size** with the full supported character
  and weight set. See [font provenance and reproduction](../design-system/fonts/README.md)
  for how the smaller asset is produced and verified.
- **Off-screen note paragraphs defer rendering** while retaining their full text
  and accessibility, using
  [`content-visibility: auto`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/content-visibility).

## Writing a test

1. Start from an acceptance criterion in [`specs/L2.md`](specs/L2.md), written as
   Given–When–Then.
2. Write the test so that it fails for the right reason.
3. Implement the least code that makes it pass.
4. Keep the criterion, the test, and the implementation aligned. If behaviour
   changes, all three change together.

If a Playwright test needs a new selector, the page object gains a method. The
test does not gain a selector.
