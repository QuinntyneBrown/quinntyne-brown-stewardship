# Stewardship

The first runnable participant journey is **awaiting enrollment**: provision an account,
sign in, open a programme destination, see the not-enrolled notice, and sign out.
See [slice scope and acceptance criteria](docs/slices/awaiting-enrollment.md).

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
attempts are recorded without extending their own cooling-off window.

Sessions use random 256-bit tokens, with only their SHA-256 digest persisted. Cookies
are Secure, HttpOnly, SameSite=Strict, persistent, and scoped to one browser session
record. Each mutating authentication request needs a fresh CSRF token. Errors expose
a correlation identifier without internal exception details; that identifier is logged.

Cohort assignment, curriculum content, bookings, and note editing are subsequent
slices. If enrollment is assigned externally, this release reports the cohort's mentor
and start date rather than incorrectly claiming the participant is unenrolled.
