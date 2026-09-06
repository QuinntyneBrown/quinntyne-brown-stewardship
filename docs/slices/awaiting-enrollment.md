# Awaiting enrollment

First participant release: provision an account, sign in, see an explicit enrollment
notice on curriculum, module, sessions, or notes routes, return later, and sign out.
Cohort assignment and enrolled programme screens are excluded. The application reads
real enrollment records; browser acceptance tests substitute service contracts only.

## Acceptance criteria

| ID | Given | When | Then | Requirements |
| --- | --- | --- | --- | --- |
| AE-01 | An authenticated participant without an active cohort | Curriculum is opened | The not-enrolled notice appears, without path, progress, or booking controls | L2-007 AC1 |
| AE-02 | The same participant | A module or sessions deep link is opened | The same notice appears without an error | L2-007 AC2 |
| AE-03 | An unidentified visitor | A protected URL or enrollment API is requested | Sign-in retains a safe internal destination; the API returns 401 | L2-004 AC1–3 |
| AE-04 | A provisioned account | Valid credentials are submitted | A secure device session is established and the retained destination opens | L2-001 AC1, L2-037 AC2–3 |
| AE-05 | Empty, invalid, unknown, or incorrect credentials | Sign-in is attempted | Field validation or the identical generic refusal appears; unknown accounts perform password verification too | L2-001 AC2–4, L2-036 AC1,4 |
| AE-06 | Ten account failures within fifteen minutes | Another attempt is made | It is refused until cooling-off ends, independently of password correctness; failures record time and origin | L2-038 AC1–4 |
| AE-07 | A saved device session | The participant returns within fourteen days, or after thirty idle days | The former remains signed in; the latter receives 401 and sign-in | L2-002 AC1–3 |
| AE-08 | Two signed-in devices | One signs out and navigates back | That device shows no programme content; the other session remains valid | L2-003 AC1–3 |
| AE-09 | A loading, failed, or expired enrollment request | The screen renders or retry is selected | Status is announced, failures can be retried, and expiry returns to sign-in | L2-007 supporting error paths |
| AE-10 | A keyboard or mobile user | Either screen is used | Controls remain reachable, labelled, visibly focused, and usable without horizontal scrolling | L2-029–034, limited to these screens |

## Traceability

Fully delivers L2-007 (L1-002). Implements the access/session criteria above from
L2-001–004 (L1-001), and relevant boundary criteria from L2-035 AC4, L2-036 AC1/4,
L2-037 AC1–4, L2-038 AC1–4 (L1-009), and L2-040 AC3 (L1-010).
No claim is made for other-owner module/note/booking behavior, programme performance,
health checks, booking audits, or unimplemented programme screens.
Responsive/accessibility coverage is L2-029 AC4–5, L2-030 AC1/3, L2-031 AC1–3
(text and controls), L2-032 AC1–2, L2-033 AC1/2/4, and L2-034's text-state rule,
limited to sign-in and enrollment notices (L1-007/008). L2-005 AC2 is exercised only
for participant-scoped enrollment lookup, not for the rest of the programme.
The enrollment payload also touches L2-006 AC1–2 by deriving the current week and
six-session allowance; the database enforces at most one active enrollment per
participant. Neither enrolled programme behavior nor cohort assignment is claimed.

Designs touched: [resolve cohort](../detailed-designs/enrollment/resolve-cohort/README.md),
[sign in](../detailed-designs/access/sign-in/README.md),
[maintain session](../detailed-designs/access/maintain-session/README.md),
[guard routes](../detailed-designs/access/guard-routes/README.md),
[secure boundary](../detailed-designs/platform/secure-boundary/README.md),
[operate and observe](../detailed-designs/platform/operate-and-observe/README.md),
[responsive shell](../detailed-designs/platform/responsive-shell/README.md), and
[accessible interaction](../detailed-designs/platform/accessible-interaction/README.md).
Visual references: [sign in](../mocks/sign-in.html), [not enrolled](../mocks/not-enrolled.html),
and [state legend](../mocks/state-legend.html). Existing mock files remain design inputs.

ATDD starts with an HTTP acceptance test expecting 401 from `/enrollment` against
an empty API host (actual 404). Subsequent acceptance tests cover the journey above.

## Implementation notes

The slice adds .NET 10, SQL Server migrations, an account-provisioning CLI, Angular 21,
and a separately built static design system. MediatR is pinned to 12.5.0. HTTP adapters
and all consumer contracts use the prescribed interface/token split. ASP.NET Core's
authentication handler implements the design's session middleware behavior. A small
assigned-cohort notice prevents a false not-enrolled claim if enrollment is later
inserted externally; this does not implement an enrolled curriculum screen.

The mockup's unresolved contact sentence is omitted. The independent design system
owns `--qbs-*` tokens; its fonts and token values are mirrored into the application.
See the [run and verification instructions](../../README.md) for provisioning,
migrations, HTTPS startup, deployment artifacts, and test commands.

## Verification — 6 September 2026

- API ATDD red: protected enrollment returned 404 instead of 401; final: **17 passing** SQL Server integration tests.
- Browser ATDD red: the sign-in heading was absent; final: **32 passing** Playwright cases, with mocked service bindings, keyboard/accessibility checks, and 320/576/768/992/1440px viewport coverage.
- **Two passing** independent design-system browser tests; production HTTP-adapter contract checks also pass.
- All three Angular libraries, the application, design-system site, and .NET Release build succeed. The API publish artifact was created successfully.
- Live HTTPS smoke check passed against the real SQL Server: CLI provisioning → sign-in → persisted session → absent enrollment → sign-out → 401. Desktop/mobile screenshots were visually reviewed.
