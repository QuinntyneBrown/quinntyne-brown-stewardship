# Stewardship

**A responsive web application for teaching people to build redemptive
technology — through a structured curriculum, learning modules, and one-on-one
mentorship.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](global.json)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)](frontend/package.json)
[![Node](https://img.shields.io/badge/Node-22.21%2B-339933?logo=nodedotjs&logoColor=white)](https://nodejs.org/)
[![PRs welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Code of Conduct](https://img.shields.io/badge/Code%20of%20Conduct-Contributor%20Covenant%202.1-5e5e5e.svg)](CODE_OF_CONDUCT.md)

Stewardship delivers a twelve-module participant curriculum, records progress
section by section, schedules six mentor conversations across twelve weeks, and
keeps private notes that carry a participant's thinking from a module into the
session it is meant to inform.

It is built as a reference-quality full-stack application: Clean Architecture on
.NET, a signal-based Angular workspace, an independent design system, and an
acceptance test suite written before the code it verifies.

---

## Table of contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Repository layout](#repository-layout)
- [Documentation](#documentation)
- [Testing](#testing)
- [Project status](#project-status)
- [Contributing](#contributing)
- [Security](#security)
- [Support](#support)
- [License](#license)

## Overview

Learning to build technology that repairs more than it costs is not a reading
exercise. It takes a path through material, work recorded honestly along the way,
and someone further along to talk to.

Stewardship gives a participant one place for all three. A cohort determines the
curriculum they follow, the dates the programme runs, the cadence of their
sessions, and the mentor they meet. Everything the interface says about their
progress is derived from what they have actually completed — never authored
separately, and never optimistic.

The twelve modules of the bundled starter curriculum:

| #   | Module                    | #   | Module                       |
| --- | ------------------------- | --- | ---------------------------- |
| 01  | Begin with stewardship    | 07  | Make access ordinary         |
| 02  | Listen before you build   | 08  | Choose enough                |
| 03  | The cost of what we build | 09  | Build for dependable service |
| 04  | Repair as a discipline    | 10  | Share power through practice |
| 05  | Who is not in the room    | 11  | Measure what matters         |
| 06  | Handle data with care     | 12  | Carry the work forward       |

Curricula are data, not code. The bundled one is a starting point; import your
own.

## Features

**Curriculum and progression.** An ordered path of twelve modules with locking, a
derived current module, and a resume point. Each module delivers ordered sections
of reading material and a practice assignment. Section completion is idempotent,
and finishing every section unlocks the next module.

**One-on-one sessions.** Participants browse their mentor's published availability
and book a slot. When two participants confirm the same slot at once, exactly one
booking succeeds and the other sees refreshed availability. Bookings can be moved
or cancelled up to exactly 24 hours before the start, releasing both the slot and
the session allowance. The session count follows from the cohort's duration and
cadence rather than being set by hand.

**Notes and preparation.** Private notes attach to a module or a session, are
revision-checked against concurrent edits, and page through an opaque cursor while
keeping every full body reachable. A module's preparation prompts carry through to
the session they inform, with saved answers rendered beneath them.

**Access and enrollment.** Password sign-in with PBKDF2 at 210,000 iterations,
persistent sessions stored only as a SHA-256 digest, CSRF enforcement on mutating
requests, and sign-in throttling serialized across API instances by a SQL
application lock. A participant who belongs to no cohort is told so explicitly
rather than shown an empty programme.

**Responsive and accessible.** Every screen is usable from extra-small (<576px)
through extra-large (≥1200px). Every element, action, and destination available at
one viewport size remains reachable at every other. Meaning conveyed through
colour, size, or position is also available in text or structure.

**Operable.** `/health` reports application and database readiness. Errors return
a correlation identifier and no internal detail, and that identifier is logged.
Booking audit records carry the actor, the action, the time, and the correlation
identifier. All programme writes are transactional.

## Architecture

```text
                    ┌──────────────────────────────┐
   Participant ───► │  Angular 21 application      │
   (browser)        │  signals · token-injected    │
                    └──────────────┬───────────────┘
                                   │  same-origin HTTPS, cookie session
                    ┌──────────────▼───────────────┐
                    │  ASP.NET Core 10 API         │
                    │  Clean Architecture · MediatR │
                    └──────────────┬───────────────┘
                                   │  Entity Framework Core
                    ┌──────────────▼───────────────┐
                    │  SQL Server                  │
                    └──────────────▲───────────────┘
                                   │  migrations · provisioning · import
                    ┌──────────────┴───────────────┐
   Administrator ─► │  Stewardship CLI             │
   (terminal)       └──────────────────────────────┘
```

Four rules shape almost every file in this repository:

1. **Radical simplicity.** The least code that satisfies the acceptance criteria,
   and nothing more. Simple in design, never reduced in scope.
2. **Dependencies point inward.** `Domain` references nothing. Controllers bind,
   dispatch through MediatR, and return; they hold no logic.
3. **Components sit where their knowledge allows.** Presentational components in
   `components`, service-injecting components in `domain`, routed pages in the
   application. Every service is reached through an interface and an
   `InjectionToken`, never a concrete implementation.
4. **The design system owns the tokens.** A hard-coded hex value, dimension, or
   font stack in a component stylesheet is a defect.

Read [docs/architecture.md](docs/architecture.md) for the full picture, and
[AGENTS.md](AGENTS.md) for the rules as stated authoritatively.

## Getting started

### Prerequisites

| Requirement | Version                                           | Notes                                          |
| ----------- | ------------------------------------------------- | ---------------------------------------------- |
| .NET SDK    | 10.0.101, or a compatible newer 10.0 feature band | Pinned in `global.json`                        |
| Node.js     | 22.21, or a compatible newer Node 22 release      |                                                |
| SQL Server  | Any supported edition                             | Windows development can use SQL Server LocalDB |

### Install and run

From the repository root:

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

The `provision` command prompts twice for a password, minimum 12 characters,
without echoing it.

The application runs at **<https://localhost:7240>**. Plain HTTP on port 5240
redirects to HTTPS. Open `/curriculum`, `/modules/3`, `/sessions`, or `/notes` to
exercise a protected deep link.

The API serves its built Angular assets from its own origin, so **re-run
`npm run build` after any frontend change**.

### Run a full programme

A participant with no cohort correctly sees the "awaiting enrollment" screen. To
run an enrolled programme, provision an administrator, import a curriculum,
publish it from the authoring screens, provision a mentor, create a cohort, enroll
the participant, and publish availability:

```powershell
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision-administrator admin@example.com
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- import-curriculum backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- provision-mentor mentor@example.com "Quinntyne Brown"
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- create-cohort .local/cohort.json
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- enroll participant@example.com <cohort-id>
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- publish-availability .local/availability.json
```

The [development guide](docs/development.md) gives the JSON shapes for
`cohort.json` and `availability.json`, the full CLI reference, and guidance on
revising a curriculum without discarding recorded work.

Curriculum authoring has screens in the application, reachable only by an account
holding administrator authority; this CLI provisions that account. Cohorts,
enrollment, mentors, and availability remain CLI tasks.

## Repository layout

```text
quinntyne-brown-stewardship/
├── backend/                  .NET 10 solution
│   ├── src/
│   │   ├── …Domain/          Entities and domain rules; references nothing
│   │   ├── …Application/     Commands, queries, handlers, validators
│   │   ├── …Infrastructure/  EF Core persistence and migrations
│   │   ├── …Api/             Thin controllers, HTTP concerns, composition root
│   │   └── …Cli/             Administration commands
│   └── tests/                API acceptance tests and performance harness
├── frontend/                 Angular 21 workspace
│   └── projects/
│       ├── quinntyne-brown-stewardship/   Routed pages, routing, guards
│       ├── api/              Service contracts, tokens, HTTP adapters
│       ├── components/       Presentational components; publishable leaf
│       └── domain/           Components that inject api contracts
├── design-system/            Independent tokens, catalogue, build, and tests
├── e2e/                      Self-contained Playwright package
├── scripts/                  Build, token mirroring, and local SQL helpers
└── docs/                     Requirements, designs, guides, and mockups
```

## Documentation

| Guide                                    | Contents                                                         |
| ---------------------------------------- | ---------------------------------------------------------------- |
| [Documentation index](docs/README.md)    | Everything, indexed.                                             |
| [Development](docs/development.md)       | Local setup, CLI reference, programme provisioning.              |
| [Architecture](docs/architecture.md)     | Layering, HTTP surface, frontend structure, behaviour contracts. |
| [Testing](docs/testing.md)               | Suites, performance budgets, how to write a test here.           |
| [Deployment](docs/deployment.md)         | Artifacts, configuration, TLS, health checks, operations.        |
| [Design system](design-system/README.md) | Tokens, fonts, catalogue, publishing.                            |
| [Live walkthrough](docs/live-demo.md)    | Five-minute feature demonstration, recording, and verification.  |
| [Requirements](docs/specs/L1.md)         | Fourteen high-level and sixty-five detailed requirements.        |

## Testing

Stewardship is built with acceptance test-driven development. A change begins with
a failing acceptance test linked to explicit Given–When–Then criteria, is
implemented until the test passes, and keeps criteria, tests, and implementation
aligned thereafter.

```powershell
. ./scripts/use-local-sql.ps1
dotnet test backend/QuinntyneBrownStewardship.sln
npm --prefix e2e run install:browsers
npm run test:e2e
npm run test:adapters
npm --prefix design-system run build
npm --prefix design-system test
```

API tests run against real persistence, real password hashing, real cookies, and
real HTTP endpoints, with a controlled clock to exercise expiry and throttling.
Browser tests use Playwright with the Page Object Model — page objects know the
DOM, tests state intent, and **a selector never appears in a test**.

Performance is measured separately, against explicit budgets: 25 API scenarios
under load, and a 300 KB transfer gate plus a 2.5-second interactivity gate across
nine screens on a throttled, cold-cache browser profile.

```powershell
npm run test:performance:api
npm run test:performance:web
```

The project does not test the shape of its own codebase. Structure, layout, and
naming belong to the compiler, the formatter, and review. See
[docs/testing.md](docs/testing.md).

## Project status

Stewardship is pre-1.0 and under active development. The participant experience
described in [`docs/specs/L1.md`](docs/specs/L1.md) is complete and covered by the
acceptance suite.

Curriculum authoring is specified by `L1-011` through `L1-014` and designed under
[`docs/detailed-designs/administration`](docs/detailed-designs/administration/), and
its implementation is in progress. The remaining administrator tasks — managing
cohorts, enrolling participants, setting mentor availability, provisioning mentors —
stay out of scope for the current requirement set and are served by the CLI.

Breaking changes may occur in minor versions before 1.0. They are announced in
[CHANGELOG.md](CHANGELOG.md).

## Contributing

Contributions are welcome. Start with [CONTRIBUTING.md](CONTRIBUTING.md), which
covers the development workflow, the ATDD approach, the architectural rules a
review will hold you to, and the Developer Certificate of Origin sign-off.

For anything larger than a bug fix, open an issue first. Behaviour is agreed as a
requirement before it is written as code.

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Governance
and the maintainer model are described in [GOVERNANCE.md](GOVERNANCE.md), and the
people behind the project in [CONTRIBUTORS.md](CONTRIBUTORS.md).

## Security

**Please do not report security vulnerabilities through public GitHub issues.**

Report them privately through
[GitHub Security Advisories](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/security/advisories/new)
or by email to **quinntynebrown@gmail.com**. [SECURITY.md](SECURITY.md) describes
the process, our response targets, and the security model already implemented.

## Support

Questions and ideas belong in
[Discussions](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/discussions);
defects belong in [Issues](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues).
See [SUPPORT.md](SUPPORT.md) for what to include and what response to expect.

## License

Stewardship is licensed under the [MIT License](LICENSE).

Bundled fonts — Karla and Newsreader — are licensed separately under the SIL Open
Font License, whose text ships alongside them in
[`design-system/fonts/`](design-system/fonts/).
