# Governance

This document describes how decisions are made in the Stewardship project, who
makes them, and how that changes over time. It is deliberately short. The project
is small, and governance should not outweigh the work it governs.

## Roles

### Contributor

Anyone who opens an issue, comments on one, improves the documentation, or sends
a pull request. No application and no invitation are required — see
[CONTRIBUTING.md](CONTRIBUTING.md).

### Maintainer

A contributor with commit access. Maintainers:

- review and merge pull requests;
- triage and label issues;
- cut releases and publish security advisories;
- uphold the architectural rules in [AGENTS.md](AGENTS.md);
- enforce the [Code of Conduct](CODE_OF_CONDUCT.md).

Maintainers are listed in [CONTRIBUTORS.md](CONTRIBUTORS.md).

### Project lead

The project lead is the final decision-maker on scope, architecture, and
releases, and resolves disagreements that maintainers cannot settle among
themselves. The lead is **Quinntyne Brown** ([@QuinntyneBrown](https://github.com/QuinntyneBrown)).

## Becoming a maintainer

Maintainership is granted by the project lead, on the strength of a sustained
record of merged contributions and of review comments that improved other
people's changes. There is no fixed threshold and no application process. What
counts is demonstrated judgement about this codebase: understanding why MediatR
is pinned, why a presentational component may not inject a service, and why a
test may not contain a selector.

Maintainers who become inactive for an extended period may be moved to an emeritus
listing. This is a housekeeping step, not a judgement, and it is reversed on
request.

## How decisions are made

**Ordinary changes** — bug fixes, documentation, tests, and features that fit an
existing requirement — are decided by review. One maintainer approval and a green
verification suite are enough to merge.

**Changes to requirements** are decided before code is written. The behaviour is
agreed in an issue, written into [`docs/specs/`](docs/specs/) as Given–When–Then
criteria, and only then implemented. A pull request that changes behaviour without
changing the requirement will be asked to do both.

**Architectural changes** — a new dependency, a change to the layering, a change
to the design token contract, or anything touching the rules in
[AGENTS.md](AGENTS.md) — need the project lead's agreement. Record the outcome as
an Architecture Decision Record so the reasoning survives the discussion.

**Disagreements** are settled by discussion in the issue or pull request. Where
discussion does not converge, the project lead decides, and says why.

## Scope

The current requirement set covers the **participant** experience: account and
access, cohort enrollment, curriculum progression, module delivery and completion,
one-on-one session scheduling, notes and session preparation, responsive
presentation, accessibility, security, and operations.

The mentor and administrator experience — authoring curriculum, managing cohorts,
setting availability, reviewing participant progress — is deliberately out of
scope for this revision and is served by the CLI. It will be added as a later set
of high-level requirements. Proposals for administrator screens will be held
against that future set rather than merged into the participant application.

Contributions are evaluated against one standard above all others: the least code
that satisfies the acceptance criteria, and nothing more. Simple in design, never
reduced in scope.

## Releases

Releases follow [Semantic Versioning](https://semver.org/). The project is
pre-1.0, so breaking changes may occur in minor versions; they are announced in
[CHANGELOG.md](CHANGELOG.md).

A release is cut when the maintainers agree the work on `main` is coherent and
the full verification suite passes, including the performance budgets. Security
fixes are released as soon as they are ready, on the schedule in
[SECURITY.md](SECURITY.md).

## Code of Conduct

The [Code of Conduct](CODE_OF_CONDUCT.md) applies to every project space.
Enforcement is the responsibility of the maintainers, and reports go to
**quinntynebrown@gmail.com**. Reports are handled confidentially.

## Changing this document

Changes to governance are proposed as a pull request and decided by the project
lead.
