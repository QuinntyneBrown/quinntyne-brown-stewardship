# Programme completion

This delivery completes the participant programme described by `docs/specs/L1.md`
and all forty requirements then in `docs/specs/L2.md`. Programme administration uses
the CLI; mentor access is an authenticated read API. No administration UI is
introduced by this delivery; curriculum authoring is specified separately by `L1-012`
and designed under `docs/detailed-designs/administration`.

## Acceptance slices

| Slice | Requirements | Given–When–Then criteria |
| --- | --- | --- |
| Access and enrollment | L2-001–007, L2-035–038 | Given an identified participant, when a programme screen opens, then only the active cohort is shown; absent enrollment remains explicit. |
| Curriculum | L2-008–011 | Given twelve ordered modules, when the curriculum opens, then completion determines the current module, locks, resume point and rounded progress. |
| Learning | L2-012–016 | Given an accessible section, when completion is submitted twice, then one completion is stored and the next section becomes current; finishing every section unlocks the next module. |
| Booking | L2-017–019, L2-023–024 | Given published mentor availability, when participants concurrently confirm a slot, then exactly one booking succeeds and the loser sees refreshed availability. A participant holds at most one future booking. |
| Booking changes | L2-020–021, L2-040 | Given a booking more than 24 hours away, when it is changed or cancelled, then the old slot is released and the change and audit are committed together. At exactly 24 hours the action is refused. |
| History and preparation | L2-022, L2-027 | Given a session whose start has passed, when history opens, then it shows the module current at session start. Preparation pairs module prompts with saved answers. |
| Notes | L2-025–028, L2-035–037 | Given a participant note attached to one module or session, when it is saved and revised, then its text and revision persist; the owner and assigned mentor can read it and unrelated identities receive 404. |
| Responsive experience | L2-029–034 | Given any screen from XS to XL, when it is operated by keyboard or touch, then all content and actions remain available with visible focus, accessible contrast and text states. |
| Operations | L2-039–040 | Given a running release, when health and measured workflows run, then database status, correlation and audit evidence are available and the specified performance budgets are measured. |

## Architecture

The existing designs under `docs/detailed-designs/` cover access, enrollment,
curriculum, learning, sessions, notes and platform. The implementation retains
Clean Architecture, MediatR 12.5.0, Angular service
contracts and tokens, and the independent design system's authoritative `--qbs-*`
tokens. Acceptance tests prove behavior; the traceability table is documentation,
not an architecture test.

## Defaults

The starter curriculum contains twelve finished modules with five sections each
(a programme holds whatever has been authored into it; see
[curriculum-authoring.md](curriculum-authoring.md)),
practice steps, effort estimates and preparation prompts. Its framing is practical
and accessible. The cohort time zone defaults to `America/Toronto`; instants are
stored in UTC. Slots default to 45 minutes. Note bodies are plain text with a
configurable 10,000-character maximum. Imports preserve stable identifiers and
reject deletion of referenced content. Module completion is derived, never stored
as an independent flag. Historical module resolution uses completion timestamps.

## Verification evidence

See [the requirement audit](requirements-audit.md) and the
[final verification record](../verification/2026-09-06.json) for inspected
implementation, corrections and results. The production build, 47 API cases,
58 browser cases, two design-system cases and adapter checks pass. All 25 API
performance scenarios and nine complete screen transfers meet their budgets.
Curriculum is usable in 1700.5ms; the largest screen transfers 276,757 bytes.

The local SQL helper resolves the Windows ARM64/LocalDB architecture mismatch
through its named pipe. API and browser performance commands are documented in
the repository README and run separately from other tests and builds.
