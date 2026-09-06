# Participant requirements audit

This audit uses `docs/specs/L1.md`, all forty L2 requirements and `AGENTS.md` as
the scope. Administration screens remain outside the participant requirements.
The source and behavioral evidence below were inspected on 2026-09-06. All forty
participant requirements are implemented and their verification gates pass. The
performance evidence covers the documented normal-load and device simulation.

## Evidence

- [Recorded verification results](../verification/2026-09-06.json) preserve the
  successful build, 47 API cases, 58 browser cases, two design-system cases,
  adapter checks, 25 API performance scenarios and nine complete screen transfers.
  The large-note payload and sign-in latency gaps found earlier are closed.
- `backend/tests/QuinntyneBrownStewardship.Api.Tests/AccessAcceptanceTests.cs`
  exercises real passwords, cookies, expiry, throttling, safe errors and enrollment.
- `backend/tests/QuinntyneBrownStewardship.Api.Tests/ProgrammeAcceptanceTests.cs`
  exercises curriculum progression, booking races/cutoffs, cohort isolation, notes,
  preparation and derived session figures through HTTP with real SQL persistence.
- `backend/tests/QuinntyneBrownStewardship.Api.Tests/AdministrationAcceptanceTests.cs`
  exercises provisioning and curriculum imports, including reopening completion after
  adding a section. `OperationsAcceptanceTests.cs` exercises database health.
- `e2e/specs/awaiting-enrollment.spec.ts` and `e2e/specs/programme.spec.ts` exercise
  the Angular service-token mock composition using screen page objects, on desktop
  and mobile. The programme checks include all five viewport breakpoints, axe,
  interactive target sizes/overlap and keyboard operation.
- `frontend/tests/adapter-acceptance.ts` checks production adapter requests and error
  contracts against Angular's HTTP testing backend.
- API performance results are in `.local/api-performance.json`; browser measurements
  and resource timings are in `.local/web-performance.json`. Commands and the device
  simulation are documented in the repository README. Run these checks separately.

## Requirement coverage

| Requirement | Implementation and inspected behavioral evidence                                                                                                                                                       |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| L2-001      | Sign-in validation, identical credential failures and real/dummy password verification; Access acceptance and sign-in browser journeys.                                                                |
| L2-002      | Persisted per-device session tokens, refreshed bounded idle expiry and 401; Access clock-driven acceptance and browser return/expiry journeys.                                                         |
| L2-003      | Session revocation affects one device; browser sign-out/back navigation is guarded; Access two-device acceptance and sign-out browser journeys.                                                        |
| L2-004      | Authorized controllers and child route guard; direct protected URLs and safe return destinations are exercised in API/browser acceptance.                                                              |
| L2-005      | Active enrollment and curriculum key scope every programme query; unique active enrollment and explicit foreign-cohort HTTP tests.                                                                     |
| L2-006      | Cohort derives weeks, end date and six-session allowance; local-zone week, ended-cohort booking and cohort setup acceptance.                                                                           |
| L2-007      | Enrollment gate renders the same explicit absence notice on programme destinations; empty programme content is absent in browser assertions.                                                           |
| L2-008      | Ordered module path with complete/current/locked labels; API completes all twelve modules and checks final states; browser checks twelve markers.                                                      |
| L2-009      | Completion counts and rounded percentages derive from section records; API checks every module transition and browser checks displayed counts.                                                         |
| L2-010      | Current module is the first incomplete module; API refuses skipped modules/sections and continues to permit completed module reads.                                                                    |
| L2-011      | Reader returns the first incomplete section, or the last after full completion; API and reload/resume browser acceptance. There is no independent module-complete flag.                                |
| L2-012      | Reader returns ordered titled sections; module template renders reading, assignment and text states; API/browser module acceptance.                                                                    |
| L2-013      | Transactional idempotent completion and persisted reload; API duplicate completion and browser complete/resume journeys.                                                                               |
| L2-014      | Native progress value/max derive from section records. Browser acceptance checks the exact “Section N of 5” label, every fill from 0% to 80%, and 100% after reopening the completed module. |
| L2-015      | One assignment source at every viewport; browser compares reading, effort and all three practice steps at XS and XL.                                                                                   |
| L2-016      | Completion is derived, including reopening on appended sections; Administration import acceptance and all-module HTTP progression.                                                                     |
| L2-017      | Cohort-local day/week availability returns open/taken states without other participants' identity; booking HTTP checks and slot-picker browser journeys.                                               |
| L2-018      | Booking response and curriculum agree on time, mentor and duration; HTTP comparison and browser booking/next-session journey.                                                                          |
| L2-019      | Transaction checks one future booking; two-device race and second-booking rejection are exercised against SQL Server.                                                                                  |
| L2-020      | Rescheduling permits 24h01m and refuses exactly 24h and 23h; HTTP acceptance checks the persisted slot after each attempt.                                                                             |
| L2-021      | Cancellation releases the slot and allowance and shares the cutoff; API state/audit checks and browser cancellation journey.                                                                           |
| L2-022      | History filters past uncancelled sessions, orders descending and resolves module from historical completions; API history/context acceptance and notes links.                                          |
| L2-023      | Allowance derives from cohort duration/cadence, counts uncancelled bookings and refuses exhaustion; API exhausted/ended cohort cases.                                                                  |
| L2-024      | Transaction-owned SQL lock serializes competing claims; two-participant race proves one winner; browser conflict refresh shows the taken slot.                                                         |
| L2-025      | Note creation, revision, stale-write protection and unsaved navigation warning; API note acceptance and browser draft/edit journey.                                                                    |
| L2-026      | Notes retain exactly one attachment, appear inline and at the notes destination, with newest-first order and empty states; API validation and browser notes journey.                                   |
| L2-027      | Session preparation joins module prompts to their answering notes; API checks saved answers and no-prompts response; browser renders answer beneath prompt.                                            |
| L2-028      | Owner/assigned-mentor read authorization and owner-only edits; explicit owner, foreign participant, assigned mentor and foreign mentor HTTP requests.                                                  |
| L2-029      | Fluid layout and wrapping checked at 320, 576, 768, 992 and 1440 pixels for all six programme screens, plus sign-in/enrollment.                                                                        |
| L2-030      | Shared navigation offers Curriculum/Sessions/Notes at every breakpoint; mobile navigation and module back link remain available.                                                                       |
| L2-031      | Axe text contrast plus explicit disabled-section text measurement; corrected opacity after a measured 4.03:1 failure against 4.5:1.                                                                    |
| L2-032      | Browser measures buttons, links, inputs, selects and textareas at all breakpoints for minimum 44px targets and non-overlapping hit areas. Inline prose links are exempt.                               |
| L2-033      | Native semantic controls, visible focus, skip link and modal dialog; keyboard sign-in and booking/cancellation-dialog focus acceptance.                                                                |
| L2-034      | Module, section and slot states use visible text/symbols, with pressed/current/disabled semantics; module/slot templates and accessibility journeys.                                                   |
| L2-035      | Acting identity comes from the authenticated session; explicit forged participant/cohort/module/booking/note identifiers cannot read or mutate foreign state.                                          |
| L2-036      | MVC binding and Application validators reject missing, malformed, oversized and inconsistent fields; API tests cover malformed route identifiers on seven read/write routes, invalid notes and literal-markup browser rendering. |
| L2-037      | HTTPS redirect, secure cookie attributes, salted one-way passwords and safe correlated errors; Access acceptance exercises these boundaries.                                                           |
| L2-038      | SQL-serialized account/origin cooldown with recorded attempts and controlled expiry; Access throttle acceptance.                                                                                       |
| L2-039      | All 21 API operations and four populated Unicode read scenarios pass their 100-request p95 gates; all nine cold-load screens pass 300KB including API payloads; curriculum is usable in 1700.5ms. |
| L2-040      | Health reports database readiness and safe 503; failures log/return the same correlation ID; booking actions commit actor/action/time/correlation audit records.                                       |

## Changes made during this audit

The disabled-opacity token now preserves state-label contrast. The curriculum shell
and its primary page load together, and the child guard performs the session check
once per navigation. Displayed curriculum totals and the session allowance consume
the returned data. Progress-only SQL projections avoid loading all reading material
for curriculum counts, completion and booking context.

The performance project now participates in solution builds as an executable rather
than a test assembly. New browser performance commands preserve service-token mocks,
use optimized assets and record the complete cold transfer, including fonts. The
README now describes curriculum import, cohort setup, enrollment and availability.

## Completion work after the first measurements

Notes now use bounded pages with opaque timestamp/identifier cursors. API acceptance
traverses 24 long notes including timestamp ties and full-length Unicode session
notes, preserving every body and attachment. The shared domain note collection
provides “More notes” at all three destinations, retains loaded content after an
error, retries without duplicates and moves keyboard focus to the new content.
Preparation answers remain beneath their prompts and also appear in the notes
destination. The final suites pass 47 API cases and 58 browser cases.

The performance fixture now captures production-compressed DTOs for twenty long
module notes, twenty session notes, three full-length preparation answers and all
six sessions. Each of nine browser journeys records its service calls and adds the
corresponding response bytes and header allowance to its asset transfers. The
performance build retains production adapter code while overriding its tokens with
the test doubles, and uses the same Brotli quality as production. This closes the
earlier measurement gaps around empty data, missing JSON and different bundles.

Critical CSS inlining was removed after the trace showed duplicate stylesheet and
body-font requests under the no-store policy. The Newsreader asset retains its
characters and declared 300–600 weight range at its default optical size, reducing
it from 132,000 to 50,548 bytes. Off-screen note paragraphs defer rendering without
removing text from the document or accessibility tree.
Booking display queries now execute after the atomic write, reducing lock duration.
Sign-in hashes outside its global gate and rechecks cooldown under the gate before
recording the result. Attempt and session commit in one transaction; acceptance
verifies concurrent throttling and rollback when session creation fails.

The final label audit also found that module progress said “N of 5 sections
complete” instead of the specified “Section N of 5”. A failing browser acceptance
case established the gap before the label correction, and now checks the label and
native progress fill through every completion state on desktop and mobile.

Malformed resource identifiers previously missed constrained API routes and could
fall through to the application document with 200. Route parameters now reach MVC
type binding, which returns 400 and names the invalid field. A failing-then-passing
API case covers all seven affected module, note, session and completion routes and
verifies that no programme state changes. Literal routes such as `current`,
`availability` and `history` retain precedence and pass the full API suite.

## Final performance and delivery evidence

The final isolated API run passes all 25 scenarios: 100 measured requests at five
concurrent clients per scenario after warm-up. The slowest read p95 is 104.75ms
for availability, below 300ms. The slowest write p95 is 287.31ms for note creation,
below 500ms. Sign-in p95 is 194.96ms.

All nine cold-load screen checks pass. Curriculum controls are usable in 1700.5ms,
below 2500ms. The largest complete screen is the module reader at 276,757 bytes,
below 300,000 bytes, including fonts, compressed API responses and header allowance.
The populated fixture includes forty 10,000-character Unicode notes, three
10,000-character answers, five past sessions and one future session. Full note
bodies remain reachable through pagination; no participant text was shortened to
meet the transfer gate.

`npm run build` passes for the three Angular libraries, production application,
independent design-system site and Release .NET solution with zero warnings or
errors. The design system owns the mirrored tokens and font assets. Source review
confirms inward backend dependencies, MediatR 12.5.0, thin dispatching controllers,
service-token composition and the prescribed project placement. The CLI remains a
project under `backend/src`. Current screenshots were inspected after the font
change; accessibility, target and viewport checks pass alongside browser journeys.

The [verification record](../verification/2026-09-06.json) contains the commands,
environment, every API percentile and every screen total. It supersedes the
intermediate measurements previously stored at that path.

CPU and network emulation use the documented Chromium
[CPU slowdown](https://chromedevtools.github.io/devtools-protocol/tot/Emulation/#method-setCPUThrottlingRate)
and [network conditions](https://chromedevtools.github.io/devtools-protocol/tot/Network/#method-emulateNetworkConditions)
commands. This is a reproducible lab profile, not physical-device certification.
