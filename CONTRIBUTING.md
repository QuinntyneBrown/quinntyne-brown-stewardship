# Contributing to Stewardship

Thank you for your interest in Stewardship. This project welcomes bug reports,
documentation improvements, and code contributions.

This guide explains how to get a working environment, what the project expects
from a change, and how a change gets reviewed and merged. Please read
[AGENTS.md](AGENTS.md) as well — it is the authoritative statement of the
architectural rules summarized here, and it applies to human and automated
contributors alike.

By participating, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Table of contents

- [Ways to contribute](#ways-to-contribute)
- [Development environment](#development-environment)
- [Development workflow](#development-workflow)
- [Acceptance test-driven development](#acceptance-test-driven-development)
- [Architectural rules](#architectural-rules)
- [Coding standards](#coding-standards)
- [Commit messages](#commit-messages)
- [Pull requests](#pull-requests)
- [Developer Certificate of Origin](#developer-certificate-of-origin)
- [Reporting bugs](#reporting-bugs)
- [Proposing features](#proposing-features)
- [Reporting security issues](#reporting-security-issues)

## Ways to contribute

- **Report a bug.** Use the [bug report template](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=bug_report.yml).
- **Improve the documentation.** Corrections to `docs/`, this file, or the README
  are as welcome as code.
- **Fix an issue.** Issues labelled `good first issue` are scoped to be
  approachable without deep familiarity with the codebase.
- **Propose a feature.** Open a [feature request](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=feature_request.yml)
  and agree on the requirement before writing code. Requirements come first in
  this project; see [Acceptance test-driven development](#acceptance-test-driven-development).

For anything larger than a bug fix, open an issue first. It is a short
conversation that avoids a long rewrite.

## Development environment

### Prerequisites

- [.NET SDK 10.0.101](https://dotnet.microsoft.com/download) or a compatible
  newer 10.0 feature band. The version is pinned in `global.json`.
- [Node.js 22.21](https://nodejs.org/) or a compatible newer Node 22 release.
- SQL Server. On Windows, SQL Server LocalDB is sufficient.

### First-time setup

Run from the repository root:

```powershell
npm ci
npm --prefix frontend ci
npm --prefix design-system ci
npm --prefix e2e ci
dotnet tool restore
dotnet dev-certs https --trust
```

The [development guide](docs/development.md) covers running the application,
provisioning a participant, and importing a curriculum.

## Development workflow

1. **Fork** the repository and create a branch from `main`. Name it for the
   change: `fix/session-cancel-window`, `docs/deployment-tls`.
2. **Write a failing acceptance test** that states the behaviour you intend to
   add or correct.
3. **Implement** until the test passes, and no further.
4. **Verify** the full suite (see below).
5. **Open a pull request** against `main`.

### Verification

Run these before opening a pull request. They are the same commands the
maintainers run:

```powershell
. ./scripts/use-local-sql.ps1
dotnet test backend/QuinntyneBrownStewardship.sln
npm --prefix e2e run install:browsers
npm run test:e2e
npm run test:adapters
npm --prefix design-system run build
npm --prefix design-system test
```

Performance checks are run separately, because competing work distorts the
measurements:

```powershell
npm run test:performance:api
npm run test:performance:web
```

Run the API performance command before the browser one; it refreshes the
captured response fixture the browser check replays. The
[testing guide](docs/testing.md) explains what each suite covers and what the
budgets are.

Formatting is handled by Prettier and `.editorconfig`. Run `npx prettier --write`
on the files you touched.

## Acceptance test-driven development

Stewardship is built with ATDD, and contributions are expected to follow it.

1. Requirements live in [`docs/specs/L1.md`](docs/specs/L1.md) (high-level) and
   [`docs/specs/L2.md`](docs/specs/L2.md) (detailed, with acceptance criteria).
2. Acceptance criteria are written in **Given–When–Then** form.
3. A change begins with a failing acceptance test linked to explicit criteria.
4. The implementation is the least code that makes the test pass.
5. Criteria, tests, and implementation stay aligned. If behaviour changes, the
   requirement changes with it in the same pull request.

**Back end:** integration tests against the real API, in
`backend/tests/QuinntyneBrownStewardship.Api.Tests`. They use real persistence,
real password hashing, real cookies, and real HTTP endpoints. A controlled clock
exercises expiry and throttling without waiting.

**Front end:** Playwright, using the Page Object Model. One page object per
screen, owning the selectors and the interactions. Tests state intent; page
objects know the DOM.

> **Never put a selector in a test.** If a test needs a new selector, the page
> object gains a method.

### Never write architecture tests

Do not add tests that assert the shape of the codebase rather than its
behaviour: no structure, layout, or naming tests; no banned-API scans; no
traceability tests that parse the specifications. Those constraints belong to
the compiler, the formatter, and review. A test suite exists to prove behaviour.

## Architectural rules

These are not stylistic preferences. A pull request that violates them will be
asked to change.

### Everywhere

- Implement requirements **radically simply**: the least code that satisfies the
  acceptance criteria, and nothing more. Simple in design, never reduced in
  scope.
- Apply SOLID principles.
- Organize features and behaviours into **vertical slices**.
- **One file per type.** Every class, interface, record, and enum gets its own
  file, named for the type it holds.
- Folders and namespaces agree.

### Back end

- **Clean Architecture.** Dependencies point inward. `Domain` references nothing.
- Controllers are thin: bind, dispatch through MediatR, return. No logic in a
  controller.
- Commands, queries, handlers, and validators live in `Application`.
- Controllers live in a `Controllers` folder and are namespaced
  `QuinntyneBrownStewardship.Api.Controllers`.
- **MediatR stays pinned to 12.5.0.** Do not upgrade it. 12.5.0 is the last
  release under plain Apache-2.0; from 13.0.0 MediatR is commercially licensed
  and free only under a registered Community tier that lapses above $5M USD
  annual revenue. A pull request that bumps this pin will be closed.
- A command-line tool is another project under `backend/src`, not a second root.

### Front end

- Prefer **signals** over RxJS, and hold state in signals. Reach for RxJS only
  for genuine streams and events.
- **No single-file components.** Template, styles, and class each live in their
  own file.

#### Where a component belongs

Placement follows what a component knows, and it is not negotiable.

| Library             | May contain                                                                                                                                                                                                                                                             |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `components`        | Presentational only — buttons, cards, pills. Takes an input, emits an output, injects no application service, imports no other project. An Angular primitive like `Router` is fine; an `api` contract is not. That leaf position is what lets the library publish to npm. |
| `domain`            | Components that inject an `api` contract through its token and render what it returns.                                                                                                                                                                                   |
| Application project | Routed page components, composing the other two and owning routing, guards, and dialogs.                                                                                                                                                                                 |

Dependencies run one way: application → `domain` → `api`. A presentational
component that turns out to need a service moves to `domain`.

#### Interface-driven service consumption

Every service the application consumes is reached through an interface and an
`InjectionToken`. No component, store, or feature imports a concrete
implementation.

- `IQuoteService` declares the contract and `QUOTE_SERVICE` is its
  `InjectionToken`. The interface, the token, and each implementation live in
  separate files. Contract files are named `<entity>-service.contract.ts`, for
  example `quote-service.contract.ts`.
- Contracts are named `I<Entity>Service`, singular, with no `Api` suffix. Data
  shapes (`QuoteResult`) take no prefix, and the production implementation takes
  the unprefixed name (`QuoteService`), never an `Impl` suffix.
- Consumers call `inject(QUOTE_SERVICE)` only. Composition binds the token to the
  HTTP adapter in production and to a mock under Playwright, so a test never
  reaches the real implementation.
- HTTP calls and observable-to-signal conversion stay inside the `api`
  implementations. `domain` types carry no HTTP dependency.

### Design system

`design-system/` is a deliverable in its own right, with its own `package.json`,
tests, and build. It owns the design tokens — colour, spacing, type scale, radius
— as CSS custom properties under the `--qbs-` prefix, and that copy is
authoritative. The front end mirrors them with `npm run tokens`.

> A hard-coded hex value, dimension, or font stack in a component stylesheet is a
> defect. Add the missing token to `design-system/tokens.css` first, then read it
> as `var(--qbs-<role>)`.

## Coding standards

- `.editorconfig` governs indentation and line endings: two spaces generally,
  four in C#, LF, UTF-8, final newline.
- C# uses file-scoped namespaces and nullable reference types.
- TypeScript is formatted by Prettier 3.8.2, pinned in the root `package.json`.
- Write comments that explain why, not what, and only where the reason is not
  evident from the code.
- Match the surrounding code. New code should be indistinguishable in style from
  the file it lives in.

## Commit messages

Use the [Conventional Commits](https://www.conventionalcommits.org/) prefixes
already present in the history:

```text
feat: add curriculum, learning progress, sessions, and notes
fix: align progress labels and validate programme route identifiers
docs: describe forwarded-header configuration for proxy deployments
refactor: rename interface files to .contract.ts convention
test: cover concurrent booking of the same slot
chore: pin Playwright to 1.58.2
```

Write the subject in the imperative mood, under 72 characters, with no trailing
period. Use the body to explain why the change was needed and what a reviewer
should look at. Reference issues with `Closes #123`.

## Pull requests

A pull request is ready for review when:

- [ ] It addresses one concern. Unrelated changes go in separate pull requests.
- [ ] It includes an acceptance test that fails without the change.
- [ ] The full verification suite passes locally.
- [ ] Requirements in `docs/specs/` are updated if behaviour changed.
- [ ] Documentation is updated if setup, operation, or public behaviour changed.
- [ ] `CHANGELOG.md` has an entry under **Unreleased** for anything a user would
      notice.
- [ ] Commits are signed off (see below).

Fill in the pull request template. Describe what changed and why, and name the
acceptance criteria the change satisfies.

Maintainers aim to give first feedback within a week. Review is a conversation:
expect questions, and ask your own. A change is merged when a maintainer approves
it and the verification suite passes.

## Developer Certificate of Origin

Contributions are accepted under the [MIT License](LICENSE). To confirm that you
have the right to submit your contribution under that license, sign off each
commit:

```sh
git commit -s -m "fix: release the slot when a booking is cancelled"
```

This appends a line to your commit message:

```text
Signed-off-by: Your Name <your.email@example.com>
```

The sign-off certifies the [Developer Certificate of Origin 1.1](https://developercertificate.org/).
Use your real name and an address you can be reached at. There is no separate
contributor licence agreement to sign.

## Reporting bugs

Open a [bug report](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=bug_report.yml)
and include what you expected, what happened, the exact commands you ran, your
.NET and Node versions, and the correlation identifier from the error response if
the API returned one.

## Proposing features

Open a [feature request](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=feature_request.yml).
State the participant need before the solution, and express the behaviour you
want as Given–When–Then criteria if you can. Note that curriculum authoring is in
scope and specified by `L1-011` through `L1-014`, while the remaining
administrator tasks — managing cohorts, enrolling participants, setting mentor
availability, provisioning mentors — stay out of scope for the current
requirement set and are handled by the CLI.

## Reporting security issues

**Do not open a public issue for a security vulnerability.** Follow the process
in [SECURITY.md](SECURITY.md).
