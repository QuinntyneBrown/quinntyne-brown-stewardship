# Stewardship

Stewardship delivers a twelve-module participant curriculum, recorded section progress,
six mentor conversations over twelve weeks, and private module/session notes with
preparation prompts. Participants without a cohort see an explicit enrollment notice.
See [programme acceptance slices](docs/slices/programme-completion.md) and the
[binding requirements](docs/specs/L2.md).

## Run locally

Prerequisites: .NET 10 SDK, Node 22.21 or a compatible newer Node 22 release, and SQL
Server. Windows development can use SQL Server LocalDB. Run from the repository root:

```powershell
npm ci
npm --prefix frontend ci
npm --prefix design-system ci
npm --prefix e2e ci
dotnet tool restore
dotnet dev-certs https --trust
. ./scripts/use-local-sql.ps1
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- migrate
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision participant@example.com
npm run build
dotnet run --project backend/src/QuinntyneBrownStewardship.Api --configuration Release --no-build
```

The CLI prompts twice for a password (minimum 12 characters) without echoing it.
The application runs at **https://localhost:7240**. Plain HTTP on port 5240 redirects
to HTTPS. Open `/curriculum`, `/modules/3`, `/sessions`, or `/notes` to exercise a
protected deep link. The application serves its built Angular assets from the API's
origin. Re-run `npm run build` after frontend changes.

To run an enrolled programme, import the bundled curriculum, provision its mentor,
create a cohort and enroll the participant. The mentor password is prompted securely,
just like the participant password:

```powershell
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- import-curriculum backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision-mentor mentor@example.com "Quinntyne Brown"
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- create-cohort .local/cohort.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- enroll participant@example.com 0932e62e-42a4-471d-8b5d-c2939b66dd99
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- publish-availability .local/availability.json
```

Create `.local/cohort.json` using the following shape. Choose a start date for your
programme; the duration and allowance are derived automatically.

```json
{
  "id": "0932e62e-42a4-471d-8b5d-c2939b66dd99",
  "startDate": "2026-09-07",
  "mentorEmail": "mentor@example.com",
  "curriculumKey": "starter",
  "timeZone": "America/Toronto"
}
```

Create `.local/availability.json` using this shape. Publish future, nonoverlapping
times inside the cohort dates, with explicit UTC offsets and stable identifiers:

```json
{
  "mentorEmail": "mentor@example.com",
  "slots": [
    {
      "id": "43b7e4bd-d139-413f-9a35-b91ec679459f",
      "startsAt": "2026-09-10T14:00:00-04:00",
      "durationMinutes": 45
    }
  ]
}
```

Repeating an import preserves identities and completion records. To revise curriculum,
retain existing module/section/prompt identifiers and order; append sections rather
than deleting recorded work. New sections reopen derived module completion. Booked
availability cannot be moved through an import. Cohort and content administration
use this CLI; the participant application contains no administrator screens.

For another SQL Server, set `ConnectionStrings__Stewardship` instead of using the
LocalDB helper. The API and CLI consume the same environment setting. The helper uses
the running LocalDB named pipe, including on Windows ARM64; dot-source it again after
restarting LocalDB because that pipe name changes.

## Verify

```powershell
. ./scripts/use-local-sql.ps1
dotnet test backend/QuinntyneBrownStewardship.sln
npm --prefix e2e run install:browsers
npm run test:e2e
npm run test:adapters
npm --prefix design-system run build
npm --prefix design-system test
npm run test:performance:api
npm run test:performance:web
```

API acceptance tests create and delete a uniquely named SQL Server database. The login
used by `STEWARDSHIP_TEST_SQL` must be allowed to create test databases. No existing
application database is cleared. Tests use real persistence, passwords, cookies, and
HTTP endpoints; a controlled clock exercises expiry and throttling without waiting.

`e2e/` is a self-contained Playwright package: its own `package.json`, lock file,
`playwright.config.ts`, and `tsconfig.json` sit beside `page-objects/` and `specs/`,
and `npm run test:e2e` delegates to it. It binds service tokens to mocks in its
dedicated Angular build and refuses authentication/enrollment HTTP calls. It uses
port 4317. The separate adapter check uses the production adapters with Angular's
HTTP testing backend to verify their requests, CSRF headers, response handling, and
errors. Design-system tests are a separate Playwright package under `design-system/`
and use port 4318. HTML reports and screen captures are retained under
`e2e/playwright-report/` and `e2e/test-results/`; traces are retained on failure.

Run performance checks separately from builds and other test suites so competing
work does not distort the normal-load measurements. The API check sends 100 measured
requests per operation from five authenticated participants against a temporary SQL
Server database, using the Release ASP.NET HTTP test host. It measures actual section
completion, note creation/revision, booking, rescheduling, cancellation, sign-in and
sign-out alongside every read endpoint, enrollment and health: 21 operations in
total, plus four compressed reads with full-length Unicode notes (25 measured
scenarios). Successful authentication measurements clear the isolated fixture's attempt
history between warm-up and measured requests; rate limits remain enabled and
concurrent refusals are verified separately. Results go to `.local/api-performance.json`.
For a focused diagnostic, append `--sign-in-only` directly to the performance
project's `dotnet run` arguments; the complete gate still uses all 25 scenarios.
The same command writes `.local/production-responses.json` from a separate populated
participant: twenty maximum-length Unicode module notes, twenty Unicode session notes,
three maximum-length Unicode preparation answers, five past sessions and one future session.
It records real response DTOs, server time and sizes compressed by the production
middleware, with application headers and an additional 1KB transport-header reserve.
Run the API command before the browser performance command to refresh this fixture.

The browser check builds optimized Angular assets with service-token doubles that
return those captured DTOs. Production adapter code remains in the measured bundle,
but the doubles override those tokens before services are instantiated. It serves
Brotli assets on port 4319 using quality 4,
matching production's `CompressionLevel.Optimal`. A cold-cache Chromium profile uses
4× CPU slowdown, 150ms network latency and 4Mbps down/1Mbps up. Each service double
waits for the captured server time plus the simulated latency and response transfer.
For each of nine screens, the 300KB gate adds all captured API responses actually used
to browser asset transfers, including fonts and headers. It also requires usable
curriculum controls within 2.5 seconds. JSON results, resource timings and API calls
are written to `.local/web-performance.json`. This is a repeatable lab simulation;
production adapters are checked separately and browser tests never call them.
Critical CSS inlining is disabled because its deferred stylesheet caused duplicate
CSS and body-font downloads under the application's no-store cache policy.
Newsreader uses its default optical size with the full supported character and weight
set; [font provenance and reproduction](design-system/fonts/README.md) document the
smaller asset. Off-screen note paragraphs defer rendering while retaining their full
text and accessibility, using [`content-visibility: auto`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/content-visibility).

## Design system and publishing

`design-system/` is an independent static site with its own package, build, and tests.
`npm --prefix design-system start` serves its built catalogue on port 4318. Publish
the contents of `design-system/dist` to the root of its own static-site origin.
It does not load the application. Fonts are self-hosted and include their OFL licenses.

Author colour, spacing, typography, and control tokens in `design-system/tokens.css`.
`npm run tokens` mirrors the authoritative tokens and base styles into Angular;
`npm run build` does this automatically. The `api`, `components`, and `domain`
Angular libraries each build independently; `components` has no application dependency.

After `npm run build`, create the API deployment artifact with:

```powershell
dotnet publish backend/src/QuinntyneBrownStewardship.Api --configuration Release --output .local/publish
```

Before starting a deployment, run the CLI's `migrate` command against its SQL Server,
set `ConnectionStrings__Stewardship`, `AllowedHosts`, `HttpsPort`, and configure its
HTTPS certificate. For TLS termination, configure a trusted proxy and forwarded headers
as part of that deployment; the supplied local configuration terminates HTTPS in Kestrel.
Apply migrations explicitly rather than granting schema-change permissions to the API.

## Slice defaults

MediatR stays pinned to **12.5.0**. Credentials use ASP.NET Core Identity V3's salted
PBKDF2 hash with 210,000 iterations. Options control the work factor, 30-day idle
session lifetime, and sign-in limits. Sign-in attempts are serialized with a SQL
application lock so concurrent API instances cannot race past the limits: ten failed
attempts per normalized email and 100 attempts per origin in fifteen minutes. Refused
attempts are recorded without extending their own cooling-off window. Password
verification runs outside the lock; cooldown is rechecked under the lock, and the
attempt and session commit in one transaction. Failed session creation rolls back both.

Sessions use random 256-bit tokens, with only their SHA-256 digest persisted. Cookies
are Secure, HttpOnly, SameSite=Strict, persistent, and scoped to one browser session
record. Each mutating authentication request needs a fresh CSRF token. Errors expose
a correlation identifier without internal exception details; that identifier is logged.

Notes are plain text, limited to 10,000 characters by default, and use revisions to
detect concurrent edits. Lists return bounded pages with opaque continuation cursors;
“More notes” keeps every full body reachable in the notes destination, module reader
and session preparation. Preparation answers remain beneath their prompts.
Booking changes close exactly 24 hours before the start;
cancelled sessions release their slot and allowance. All programme writes are
transactional and booking audit records carry the actor, action, time and correlation
identifier. `/health` reports application/database readiness with 200 or 503.
