# Architecture

Stewardship is a .NET 10 API serving an Angular 21 single-page application from
its own origin, backed by SQL Server, with an independent design system and a
command-line tool for operator tasks. The application serves two audiences: a
participant reading a published curriculum, and an administrator authoring it.

The governing principle is stated in [AGENTS.md](../AGENTS.md) and repeated here
because it explains most of what follows: implement requirements _radically
simply_ — the least code that satisfies the acceptance criteria, and nothing more.
Simple in design, never reduced in scope.

## Containers

```text
                    ┌──────────────────────────────┐
   Participant ───► │  Angular application         │
   (browser)        │  projects/quinntyne-brown-…  │
                    └──────────────┬───────────────┘
                                   │  same-origin HTTPS, cookie session
                    ┌──────────────▼───────────────┐
                    │  ASP.NET Core API            │
                    │  serves wwwroot + endpoints  │
                    └──────────────┬───────────────┘
                                   │  Entity Framework Core
                    ┌──────────────▼───────────────┐
                    │  SQL Server                  │
                    └──────────────▲───────────────┘
                                   │  migrations, provisioning, import
                    ┌──────────────┴───────────────┐
   Operator ──────► │  Stewardship CLI             │
   (terminal)       └──────────────────────────────┘

   An administrator authors curriculum through the web client above, over the
   same session cookie a participant uses. The CLI provisions that account and
   manages cohorts, enrollment, mentors, and availability.
```

The design system is a fourth, wholly separate deliverable: a static catalogue
site that does not load the application and carries no runtime dependency on it.

## Back end

### Clean Architecture

Dependencies point inward. `Domain` references nothing.

| Project          | Contains                                                                                | May reference                   |
| ---------------- | --------------------------------------------------------------------------------------- | ------------------------------- |
| `Domain`         | Entities and domain rules                                                               | Nothing                         |
| `Application`    | Commands, queries, handlers, validators, and the abstractions infrastructure implements | `Domain`                        |
| `Infrastructure` | Entity Framework Core persistence, migrations, and other outward adapters               | `Application`, `Domain`         |
| `Api`            | Controllers, HTTP concerns, composition root                                            | `Application`, `Infrastructure` |
| `Cli`            | Administration commands over the same application layer                                 | `Application`, `Infrastructure` |

The command-line tool is another project under `backend/src`, not a second root.
It reaches the same handlers the API does, which is why importing a curriculum and
booking a session obey identical rules.

### Vertical slices

Features are organized as vertical slices rather than by technical layer. A slice
owns its command or query, its handler, its validator, and its response shape.
Adding a behaviour means adding a slice, not editing four horizontal folders.

### Thin controllers

A controller binds, dispatches through MediatR, and returns. There is no logic in
a controller. In practice most actions are a single expression:

```csharp
[HttpGet("modules/{ordinal}")]
public Task<ModuleResponse> Get(int ordinal, CancellationToken ct)
    => sender.Send(new GetModuleQuery(ordinal), ct);
```

Controllers live in a `Controllers` folder and are namespaced
`QuinntyneBrownStewardship.Api.Controllers`. Folders and namespaces agree
throughout the codebase, and every class, interface, record, and enum gets its own
file named for the type it holds.

### MediatR is pinned to 12.5.0

This is a licensing constraint, not a preference. 12.5.0 is the last release under
plain Apache-2.0. From 13.0.0, MediatR is commercially licensed and free only
under a registered Community tier that lapses above $5M USD annual revenue. **Do
not upgrade it.**

### HTTP surface

All endpoints require an authenticated participant except `/authentication/csrf`,
`/authentication/sign-in`, and `/health`.

| Method   | Route                        | Purpose                                           |
| -------- | ---------------------------- | ------------------------------------------------- |
| `GET`    | `/authentication/csrf`       | Issue a CSRF token                                |
| `POST`   | `/authentication/sign-in`    | Establish a session                               |
| `GET`    | `/authentication/session`    | Describe the current session                      |
| `POST`   | `/authentication/sign-out`   | End the session                                   |
| `GET`    | `/enrollment`                | The participant's cohort, or its explicit absence |
| `GET`    | `/curriculum`                | The ordered module path with derived progress     |
| `GET`    | `/modules/current`           | The module the participant is on                  |
| `GET`    | `/modules/{ordinal}`         | A module by position                              |
| `POST`   | `/sections/{id}/completion`  | Record a section as complete (idempotent)         |
| `GET`    | `/sessions/availability`     | Published mentor slots, optionally by day         |
| `POST`   | `/sessions`                  | Book a slot                                       |
| `GET`    | `/sessions/history`          | Sessions already held                             |
| `GET`    | `/sessions/{id}`             | One booking                                       |
| `GET`    | `/sessions/{id}/preparation` | Module prompts paired with saved answers          |
| `PUT`    | `/sessions/{id}/slot`        | Reschedule                                        |
| `DELETE` | `/sessions/{id}`             | Cancel, releasing the slot and allowance          |
| `GET`    | `/notes`                     | Paginated notes, filtered by module or session    |
| `GET`    | `/notes/{id}`                | One note                                          |
| `POST`   | `/notes`                     | Create or revise a note                           |
| `GET`    | `/health`                    | Application and database readiness (200 or 503)   |

Every route below requires administrator authority and is refused with `403` without
it.

| Method   | Route                                          | Purpose                                         |
| -------- | ---------------------------------------------- | ----------------------------------------------- |
| `GET`    | `/administration/curricula`                    | Every programme with its module count and state |
| `POST`   | `/administration/curricula`                    | Create a programme in draft                     |
| `GET`    | `/administration/curricula/{id}`               | One authored programme, whatever its state      |
| `PUT`    | `/administration/curricula/{id}`               | Revise the programme title                      |
| `DELETE` | `/administration/curricula/{id}`               | Remove a programme no cohort follows            |
| `PUT`    | `/administration/curricula/{id}/key`           | Correct the key while no cohort follows         |
| `PUT`    | `/administration/curricula/{id}/modules/order` | Reorder the modules of a programme              |
| `POST`   | `/administration/curricula/{id}/publication`   | Publish the programme and its modules           |
| `POST`   | `/administration/curricula/{id}/modules`       | Add a module in draft                           |
| `GET`    | `/administration/sections/{id}`                | One authored section with its reading content   |
| `GET`    | `/administration/modules/{id}`                 | One authored module                             |
| `PUT`    | `/administration/modules/{id}`                 | Revise title, summary, effort, practice steps   |
| `DELETE` | `/administration/modules/{id}`                 | Remove a module nothing recorded depends on     |
| `PUT`    | `/administration/modules/{id}/sections/order`  | Reorder the sections of a module                |
| `POST`   | `/administration/modules/{id}/sections`        | Add a section                                   |
| `POST`   | `/administration/modules/{id}/prompts`         | Add a preparation prompt                        |
| `PUT`    | `/administration/modules/{id}/prompts/order`   | Reorder the preparation prompts of a module     |
| `PUT`    | `/administration/sections/{id}`                | Revise a section                                |
| `DELETE` | `/administration/sections/{id}`                | Remove a section                                |
| `PUT`    | `/administration/prompts/{id}`                 | Revise a prompt, preserving its identifier      |
| `DELETE` | `/administration/prompts/{id}`                 | Remove a prompt                                 |

## Front end

`frontend/` is an Angular workspace. The `api`, `components`, and `domain`
libraries and the application project are siblings under `frontend/projects/`.

State is held in **signals**. RxJS is reached for only where there is a genuine
stream or event. Components are never single-file: template, styles, and class
each live in their own file.

The three libraries each build independently — `npm --prefix frontend run
build:libraries` builds `api`, then `components`, then `domain`. `components` has
no dependency on the application, which is what keeps it publishable on its own.

### Where a component belongs

Placement follows what a component knows, and it is not negotiable.

| Library             | Knows                                                                                                                                                                                                                                             |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `components`        | Presentational only. Takes an input, emits an output, injects no application service, imports no other project. An Angular primitive like `Router` is fine; an `api` contract is not. That leaf position is what lets the library publish to npm. |
| `domain`            | Injects an `api` contract through its token and renders what it returns.                                                                                                                                                                          |
| Application project | Routed page components, composing the other two and owning routing, guards, and dialogs.                                                                                                                                                          |

Dependencies run one way: application → `domain` → `api`. A presentational
component that turns out to need a service moves to `domain`.

### Interface-driven service consumption

Every service the application consumes is reached through an interface and an
`InjectionToken`. No component, store, or feature imports a concrete
implementation.

```text
curriculum-service.contract.ts   ICurriculumService — the contract
curriculum-service.token.ts      CURRICULUM_SERVICE — the InjectionToken
curriculum.service.ts            CurriculumService  — the production HTTP adapter
```

Contracts are named `I<Entity>Service`, singular, with no `Api` suffix. Data
shapes (`CurriculumResult`) take no prefix, and the production implementation
takes the unprefixed name, never an `Impl` suffix.

Consumers call `inject(CURRICULUM_SERVICE)` and nothing else. Composition binds the
token to the HTTP adapter in production and to a mock under Playwright, which is
what makes it structurally impossible for an end-to-end test to reach the real
implementation.

HTTP calls and observable-to-signal conversion stay inside the `api`
implementations, so `domain` types carry no HTTP dependency.

### Routes

| Route           | Screen                                        |
| --------------- | --------------------------------------------- |
| `/sign-in`      | Sign-in                                       |
| `/curriculum`   | The module path with progress (default route) |
| `/modules/:id`  | Module reader with sections and completion    |
| `/sessions`     | Availability, booking, and history            |
| `/sessions/:id` | Session detail and preparation                |
| `/notes`        | Paginated notes                               |
| `/notes/new`    | Note editor                                   |
| `/notes/:id`    | Note editor with unsaved-change guard         |

Authoring routes sit behind an administrator guard in addition to the auth guard.

| Route                        | Screen                                                   |
| ---------------------------- | -------------------------------------------------------- |
| `/admin/programmes`          | Programme index with publication state                   |
| `/admin/programmes/:id`      | Programme editor, module list, publish panel             |
| `/admin/modules/:id`         | Module editor with unsaved-change guard                  |
| `/admin/modules/:id/preview` | The module as a participant reads it, before publication |

Everything under the programme shell is behind an auth guard that preserves the
deep link through sign-in.

## Design system

`design-system/` is a deliverable in its own right, not a folder inside the front
end. It has its own `package.json`, tests, and build, deploys as its own static
site, and carries no runtime dependency on the application.

It owns the design tokens — colour, spacing, type scale, radius — as CSS custom
properties under the `--qbs-` prefix, and **that copy is authoritative**. The front
end mirrors them with `npm run tokens`, which `npm run build` runs automatically.
Every component stylesheet reads them as `var(--qbs-<role>)`.

> A hard-coded hex value, dimension, or font stack in a component stylesheet is a
> defect. Add the missing token to `design-system/tokens.css` first.

See [design-system/README.md](../design-system/README.md).

## Behaviour contracts

These are the decisions a reader most often needs, gathered in one place. Each is
covered by the acceptance suite.

**Identity.** Credentials use ASP.NET Core Identity V3's salted PBKDF2 hash at
210,000 iterations. Options control the work factor, the 30-day idle session
lifetime, and the sign-in limits.

**Sign-in throttling.** Attempts are serialized with a SQL application lock, so
concurrent API instances cannot race past the limits: ten failed attempts per
normalized email and 100 attempts per origin in fifteen minutes. A refused attempt
is recorded without extending its own cooling-off window. Password verification
runs outside the lock; the cooldown is re-checked under the lock, and the attempt
and the session commit in one transaction. A failed session creation rolls back
both.

**Sessions.** Random 256-bit tokens, with only the SHA-256 digest persisted.
Cookies are `Secure`, `HttpOnly`, `SameSite=Strict`, persistent, and scoped to one
browser session record. Every mutating authentication request needs a fresh CSRF
token.

**Errors.** Responses expose a correlation identifier and no internal exception
detail. The identifier is logged, so an operator can join the two.

**Notes.** Plain text, limited to 10,000 characters by default, with revisions
used to detect concurrent edits. Lists return bounded pages with opaque
continuation cursors; "More notes" keeps every full body reachable from the notes
destination, the module reader, and session preparation. Preparation answers
remain beneath their prompts.

**Progress.** Every count and proportion the interface displays is derived from
recorded completion, never authored separately. Adding a section to a module
reopens that module's derived completion.

**Booking.** Booking changes close exactly 24 hours before the start. A cancelled
session releases both its slot and the participant's allowance. Concurrent
confirmations of the same slot resolve to exactly one booking; the other
participant sees refreshed availability.

**Integrity and operations.** All programme writes are transactional. Booking
audit records carry the actor, the action, the time, and the correlation
identifier. `/health` reports application and database readiness with 200 or 503.

## Detailed designs

Per-feature designs, with C4 container and component diagrams, class structures,
and sequence diagrams, live under
[`docs/detailed-designs/`](detailed-designs/), organized by subsystem: access,
enrollment, curriculum, modules, sessions, notes, administration, and platform.
