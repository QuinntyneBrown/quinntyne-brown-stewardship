# Development guide

How to get Stewardship running on your machine, provision a participant, and load
a programme. For what the code is shaped like, see
[architecture.md](architecture.md); for how it is verified, see
[testing.md](testing.md).

## Prerequisites

| Requirement | Version                                                 | Notes                                       |
| ----------- | ------------------------------------------------------- | ------------------------------------------- |
| .NET SDK    | 10.0.101, or a compatible newer 10.0 feature band       | Pinned in `global.json`                     |
| Node.js     | 22.21, or a compatible newer Node 22 release            |                                             |
| SQL Server  | Any supported edition                                   | Windows development can use SQL Server LocalDB |

## Restore and trust

Run from the repository root:

```powershell
npm ci
npm --prefix frontend ci
npm --prefix design-system ci
npm --prefix e2e ci
dotnet tool restore
dotnet dev-certs https --trust
```

## Point at a database

On Windows, dot-source the LocalDB helper:

```powershell
. ./scripts/use-local-sql.ps1
```

The helper sets `ConnectionStrings__Stewardship` from the running LocalDB named
pipe, including on Windows ARM64. That pipe name changes when LocalDB restarts, so
dot-source the script again after a restart.

For any other SQL Server, set the environment variable yourself instead:

```powershell
$env:ConnectionStrings__Stewardship = "Server=...;Database=Stewardship;..."
```

The API and the CLI read the same setting.

## Run the application

```powershell
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- migrate
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision participant@example.com
npm run build
dotnet run --project backend/src/QuinntyneBrownStewardship.Api --configuration Release --no-build
```

The `provision` command prompts twice for a password, minimum 12 characters,
without echoing it.

The application runs at **https://localhost:7240**. Plain HTTP on port 5240
redirects to HTTPS.

`npm run build` mirrors the design tokens into Angular, builds the three libraries
and the application, builds the design system, and copies the browser bundle into
the API's `wwwroot`. The API serves its Angular assets from its own origin, so
**re-run `npm run build` after any frontend change**.

Open `/curriculum`, `/modules/3`, `/sessions`, or `/notes` to exercise a protected
deep link.

### Frontend-only iteration

For rapid UI work, Angular's dev server is faster than a full rebuild:

```powershell
npm --prefix frontend start
```

Remember that this serves from a different origin than the API. The packaged
build is what the acceptance tests and the performance harness measure.

## Provision a programme

A provisioned participant with no cohort sees the explicit "awaiting enrollment"
screen — that is correct behaviour, not an error. To run an enrolled programme,
import the bundled curriculum, provision a mentor, create a cohort, enroll the
participant, and publish availability:

```powershell
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- import-curriculum backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision-mentor mentor@example.com "Quinntyne Brown"
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- create-cohort .local/cohort.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- enroll participant@example.com 0932e62e-42a4-471d-8b5d-c2939b66dd99
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- publish-availability .local/availability.json
```

The mentor password is prompted securely, exactly like the participant password.

### `.local/cohort.json`

Choose a start date for your programme. The duration and the session allowance are
derived automatically.

```json
{
  "id": "0932e62e-42a4-471d-8b5d-c2939b66dd99",
  "startDate": "2026-09-07",
  "mentorEmail": "mentor@example.com",
  "curriculumKey": "starter",
  "timeZone": "America/Toronto"
}
```

### `.local/availability.json`

Publish future, non-overlapping times inside the cohort dates, with explicit UTC
offsets and stable identifiers:

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

`.local/` is git-ignored, which is why these fixtures live there.

## CLI reference

The participant application contains no administrator screens. Cohort and content
administration use this CLI.

| Command                                | Arguments                 | Effect                                                              |
| -------------------------------------- | ------------------------- | ------------------------------------------------------------------- |
| `migrate`                              | —                         | Applies pending Entity Framework Core migrations.                    |
| `provision <email>`                    | Email                     | Creates a participant. Prompts twice for a password, without echo.   |
| `provision-mentor <email> <name>`      | Email, display name       | Creates a mentor. Prompts twice for a password, without echo.        |
| `import-curriculum <json-file>`        | Path to curriculum JSON   | Imports or revises a curriculum. Prints the module count.            |
| `create-cohort <json-file>`            | Path to cohort JSON       | Creates a cohort. Prints its identifier.                             |
| `enroll <email> <cohort-id>`           | Email, cohort GUID        | Enrolls a participant into a cohort.                                 |
| `publish-availability <json-file>`     | Path to availability JSON | Publishes mentor slots. Prints the slot count.                       |

Running the CLI with no arguments prints this usage line and exits with status 2.

## Revising a curriculum

Repeating an import preserves identities and completion records. When revising:

- Retain existing module, section, and prompt identifiers and their order.
- **Append** sections rather than deleting recorded work.
- Expect new sections to reopen derived module completion — a module is complete
  only when every one of its sections is.
- Booked availability cannot be moved through an import.

The bundled starter curriculum is documented in
[`docs/curriculum/starter.md`](curriculum/starter.md).

## Repository scripts

| Command                       | What it does                                                                 |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `npm run build`               | Mirrors tokens, builds the libraries, application, and design system, copies assets into `wwwroot`, and builds the solution in Release. |
| `npm run tokens`              | Mirrors the authoritative design tokens and base styles into Angular.        |
| `npm run test:api`            | Runs the API acceptance tests.                                               |
| `npm run test:e2e`            | Delegates to the self-contained Playwright package in `e2e/`.                |
| `npm run test:adapters`       | Exercises the production HTTP adapters against Angular's testing backend.    |
| `npm run test:performance:api` | Measures API operations and refreshes the captured response fixture.        |
| `npm run test:performance:web` | Measures the browser experience against the captured fixture.               |

## Troubleshooting

**The UI does not reflect my change.** Run `npm run build`. The API serves built
assets from `wwwroot`.

**A connection error after restarting LocalDB.** Dot-source
`scripts/use-local-sql.ps1` again; the named pipe changed.

**The browser refuses the certificate.** Run `dotnet dev-certs https --trust`.

**An API error with no detail.** That is deliberate. The response carries a
correlation identifier; find it in the server log for the full exception.
