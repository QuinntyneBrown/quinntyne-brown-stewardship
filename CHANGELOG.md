# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No version has been tagged yet. Everything below describes the state of `main`
and will form the initial `0.1.0` release.

### Added

**Access and enrollment**

- Password sign-in with ASP.NET Core Identity V3 salted PBKDF2 hashing at 210,000
  iterations, with the work factor configurable through Options.
- Persistent sessions backed by random 256-bit tokens, of which only the SHA-256
  digest is stored. Cookies are `Secure`, `HttpOnly`, `SameSite=Strict`, and
  scoped to a single browser session record, with a 30-day idle lifetime.
- CSRF token issuance and enforcement on every mutating authentication request.
- Sign-in throttling serialized by a SQL application lock, so concurrent API
  instances cannot race past the limits of ten failed attempts per normalized
  email and 100 attempts per origin in fifteen minutes.
- Route guards that preserve deep links through sign-in.
- An explicit "awaiting enrollment" experience for a participant who belongs to
  no cohort, in place of an empty programme.

**Curriculum and learning**

- An ordered curriculum of authored modules with module locking, a derived
  current module, a resume point, and progress proportions computed from
  recorded completion rather than authored separately.
- Module delivery as ordered sections carrying reading material and a practice
  assignment, with idempotent section completion that unlocks the next module
  once every section is complete.

**Sessions**

- Mentor availability browsing and session booking, with exactly one booking
  succeeding when participants confirm the same slot concurrently.
- Rescheduling and cancellation up to exactly 24 hours before the start, with
  the released slot and the session allowance both returned.
- Session history showing the module that was current when each session started.

**Notes**

- Private plain-text notes attached to a module or a session, limited to 10,000
  characters by default and revision-checked to detect concurrent edits.
- Cursor-paginated note lists with a "More notes" affordance that keeps every
  full body reachable from the notes destination, the module reader, and session
  preparation.
- Module preparation prompts carried through to the session they inform, with
  saved answers rendered beneath their prompts.

**Platform**

- .NET 10 API following Clean Architecture, with MediatR 12.5.0, FluentValidation,
  and Entity Framework Core against SQL Server.
- Angular 21 workspace with `api`, `components`, and `domain` libraries and the
  application project, all state held in signals and every service reached
  through an `InjectionToken`.
- An independent design system with authoritative `--qbs-` design tokens, a
  static catalogue site, self-hosted OFL-licensed Karla and Newsreader fonts,
  and its own Playwright test suite.
- An administration CLI providing `migrate`, `provision`, `provision-mentor`,
  `provision-administrator`, `grant-administrator`, `revoke-administrator`,
  `import-curriculum`, `create-cohort`, `enroll`, and `publish-availability`.
  Passwords are prompted twice without echo.
- `/health` endpoint reporting application and database readiness as 200 or 503.
- Error responses carrying a correlation identifier without internal exception
  detail, with the identifier written to the log.
- Transactional programme writes, with booking and curriculum audit records
  carrying the actor, action, time, and correlation identifier.
- Responsive presentation from extra-small through extra-large viewports, with
  keyboard operability, visible focus, and accessible contrast.
- Acceptance test suites: API integration tests, Playwright end-to-end tests
  using the Page Object Model, production HTTP adapter tests, design system
  tests, and API and browser performance harnesses with explicit budgets.

**Curriculum authoring**

- Requirements `L1-011` through `L1-014` and `L2-041` through `L2-066` covering
  administrator access, curriculum authoring, publication, and derived programme
  shape.
- Detailed designs under `docs/detailed-designs/administration` for six authoring
  features, with rendered C4, class, and sequence diagrams.
- Screen mockups under `docs/mocks` for the authoring screens: the programme
  index, the programme, module, and section editors, the states in which a
  publication, a removal, or an authored field is refused, what an account
  without administrator authority is shown instead, and every one of those
  actions at phone width.
- Administrator authority held on the account and issued as a claim on every
  request, so a grant or a withdrawal reaches the next request; conferred out of
  band with `provision-administrator`, `grant-administrator`, and
  `revoke-administrator`.
- Authoring screens under `/admin`: the programme index and the programme,
  module, and section editors, behind an administrator guard, reached through the
  shell's Authoring link, and returning any other account to its curriculum with
  an explanation. A deep link survives sign-in and names only its route.
- Programmes created, retitled, re-keyed, and removed; modules, sections, and
  preparation prompts added, revised, and removed; every refusal names its field
  with the maximum and the overage, is announced, and moves focus to the field.
- Ordering of modules, sections, and prompts, kept contiguous and unique through
  a staged reassignment inside one transaction, moved by keyboard with the new
  arrangement announced.
- Publication as a deliberate act that makes every module of a programme readable
  at once, refused while any module carries no section, with the cohorts it
  reaches and the participants it would move back stated before the act. A
  module added afterwards waits for the next publication; revised text does not.
- A preview that renders the module the editor holds, unsaved changes included,
  as a participant reads it, offering no completion and recording nothing.
- Recorded history outranking removal: a completed section, a module with notes
  or answered prompts, and an answered prompt refuse removal with the reason
  named, and the participant's progress is untouched.
- A stale revision refused with the current content offered for comparison;
  unsaved authored content guarded on navigation and on tab close, and kept in
  the form when a save fails, including on an expired session.
- API performance scenarios for the authoring reads, writes, and publication, and
  the four authoring screens under the browser transfer gate.
- A programme record. Modules and cohorts follow a `Curriculum` row rather than a
  key string, a programme and each module carry a publication state, and only
  published modules reach a participant. A cohort following a draft programme is
  told the programme is not yet available.
- Cohort duration and session cadence stored on the cohort record, so the module
  count, the week number, the end date, and the session allowance derive from
  records rather than constants.

### Changed

- The bundled curriculum now teaches FaithTech's framework for building redemptive
  technology: five modules running the 4D Cycle of Discover, Discern, Develop and
  Demonstrate, preceded by Prepare, with the Co-Creation Cycle of Request, Receive,
  Review, Render and Rejoice inside Develop. It is adapted from The FaithTech Playbook
  and the FaithTech Workbook under CC BY 4.0, and FaithTech does not endorse this
  software. `npm run curriculum` compiles the manuscript into the import document.
- The README no longer prints a table of module titles. A programme holds whatever has
  been authored into it, and nothing in the application fixes a curriculum's size or
  its subject.
- The live demo runs a ten-week cohort at a fortnightly cadence — two weeks per stage
  of the cycle, and a conversation after each — so its allowance follows the cohort
  record rather than a literal.
- `import-curriculum` creates one draft programme per document and refuses a key
  already in use, instead of merging into the existing curriculum.
- `create-cohort` requires `curriculumKey`, `durationWeeks`, and
  `sessionCadenceWeeks`, and refuses a curriculum that is not yet published.
- The refusal for an exhausted session allowance states the cohort's own
  allowance rather than "six".
- The enrollment and curriculum responses carry the cohort's duration and
  cadence and whether its programme is published.
- Authored field maxima are `Curriculum` options, carried on every authoring
  read, and counted in characters as an author counts them.
- The requirement set no longer fixes the programme shape. `L2-006`, `L2-008`,
  `L2-009`, `L2-010`, `L2-021` and `L2-023` were amended so the module count, the
  cohort duration, the session cadence and the session allowance are read from
  records rather than written into the acceptance criteria.
- The screen mockups no longer present the twelve-module, twelve-week cohort as a
  fixed shape. An eight-module programme and an eight-week cohort with an
  allowance of four are drawn beside it, the sign-in copy no longer states a
  module and session count, and the curriculum sheet gained the two refusals a
  participant meets when a programme or a module is not published.
- Frontend service contract files were renamed to the `<entity>-service.contract.ts`
  convention.
- Progress labels were aligned with the values they describe, and programme route
  identifiers are now validated rather than trusted.
- Critical CSS inlining is disabled, because its deferred stylesheet caused
  duplicate CSS and body-font downloads under the application's `no-store` cache
  policy.
- Newsreader is subset to weights 300–600 at its default optical size, reducing
  the asset from 132,000 to 50,548 bytes with no loss of characters or layout
  features.

### Security

- See [SECURITY.md](SECURITY.md) for the full security model and the process for
  reporting a vulnerability privately.

[Unreleased]: https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/commits/main
