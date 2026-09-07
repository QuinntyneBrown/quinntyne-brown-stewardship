# Curriculum authoring

Administrator release of the curriculum: an account holding administrator authority
reaches `/admin/programmes`, creates a programme, authors its modules, sections, and
preparation prompts, orders them, previews a module as a participant will read it, and
publishes deliberately. Only published content reaches a participant; recorded
participant history outranks removal; every authoring write is audited. Cohorts,
enrollment, mentors, and availability remain command-line tasks. The application reads
real curriculum records; browser acceptance tests substitute service contracts only.

## Acceptance criteria

| ID | Given | When | Then | Requirements |
| --- | --- | --- | --- | --- |
| CA-01 | An eight-module document imported and published, followed by an eight-week cohort with a session every two weeks | The participant reads the curriculum, the enrollment, and availability | Eight markers, eight remaining, and an allowance of four; nothing reads twelve or six | L2-056 AC1, L2-057 AC1, L2-058 AC3–4 |
| CA-02 | A database from the previous release | The `AuthorCurriculum` migration runs | Every existing key becomes a published programme row, modules and cohorts follow it, revisions and cohort figures are populated, and the key columns are gone | L2-056, L2-057 |
| CA-03 | No identity, a participant, and an administrator | The session and the programme index are requested | 401, 403 without a `Location` header and without curriculum data, and 200 respectively; authority comes from the account row on every request | L2-041, L2-042, L2-043, L2-064 |
| CA-04 | An administrator on the index | A programme is created with a key another programme holds | The refusal names the key and is announced, focus moves to the field, the typed title stays, and nothing is stored; a free key creates the programme in draft | L2-044 AC1–2, L2-051 AC1, L2-060 AC3 |
| CA-05 | A programme no cohort follows, and one a cohort follows | Each is renamed, re-keyed, and removed | The free one takes a new key and is removed with its modules, sections, and prompts after a dialog that holds focus; the followed one refuses both, naming the cohort | L2-044 AC4–9, L2-060 AC5 |
| CA-06 | A programme whose third module has no sections | Publication is attempted | The draft already states the reason, the attempt is refused naming `03 Choose enough`, and what participants read is unchanged | L2-054 AC1–2, AC4, L2-060 AC4 |
| CA-07 | A complete draft programme a cohort follows | It is published, a module is added, and a section is revised | Every module becomes readable at once; the added module waits for the next publication while the revised text reaches the participant now; a module published beneath a participant's current one becomes current with completed modules still complete | L2-054 AC3, L2-055 AC1–5, L2-056 AC2, L2-058 AC2 |
| CA-08 | A module a participant is reading | Its title, summary, effort estimate, and steps are revised as one form | The participant reads every value as authored, or no practice block when there are no steps; a stale revision is refused with the current content offered, and the next save after reloading succeeds | L2-045, L2-046, L2-065 |
| CA-09 | A prompt a participant has answered | Its wording is revised, then its removal is attempted | The answer stays attached to the revised prompt; the removal is refused naming the answer; added and removed prompts keep a contiguous order | L2-047, L2-050 AC3 |
| CA-10 | A section a participant completed, a module with a note, and a module with an answered prompt | Each removal is attempted | Each is refused with 409 and the reason named, no constraint fails, and the participant's progress is exactly as it was | L2-050 AC1–2, AC4–7 |
| CA-11 | A module a participant has completed | A section is added, then removed | Completion reopens without rewriting the history; the remaining sections close the gap | L2-048, L2-016 AC3 |
| CA-12 | A title left empty and a summary of 442 characters | The module is saved | Every refusal names its field with the maximum and the overage, is announced, focus moves to the first refused field, and nothing is stored; exactly 12,000 four-byte characters of reading save and one more is refused | L2-051 AC2, AC6–9, L2-060 AC3 |
| CA-13 | Four modules | The fourth moves to second, by keyboard | Positions are contiguous and unique, the arrangement is announced and survives a reload, a list that is not a permutation is refused, and a failure after the staged pass leaves the prior order | L2-049, L2-047 AC5, L2-060 AC2 |
| CA-14 | Unsaved authored changes | The screen is left, the tab is reloaded, or the changes are discarded | A warning names the changed fields and staying keeps them; the browser warns on reload; discarding restores every saved value and says nothing is unsaved | L2-063 |
| CA-15 | Unsaved changes to a draft module | It is previewed and the editor is returned to | It reads as a participant reads it, paragraphs split by the same rule and markup literal, every section open, no completion or answer offered, nothing recorded, and every unsaved change still in its field | L2-066 |
| CA-16 | Every administration screen | It renders from XS to XL | It fits the viewport, every control meets its target size, the actions at XS are the actions at XL, the trail condenses to one link, states read as text, and axe reports no violation | L2-059, L2-060 AC1, AC4, L2-062 |

## Traceability

Fully delivers L1-011 through L1-014 through L2-041 to L2-066, and the amended
L2-006, L2-008, L2-009, L2-010, L2-021, and L2-023, whose module count, cohort duration,
session cadence, and session allowance are now read from records. L2-016 AC3 is
delivered through the section endpoint in place of the retired import merge. L2-061 is
covered by the 401/403/CSRF/unknown-field/validation cases in the API suite; the
64 KB request body limit is enforced by Kestrel and bounded by the `Curriculum` options
validation, and it is not asserted through the in-process test host, which does not
enforce it. L2-062 is covered by the eight authoring scenarios of the API performance
harness and the four authoring screens of the browser transfer gate, and is claimed
only for the recorded lab profile.

Designs touched: [authorise administrator](../detailed-designs/administration/authorise-administrator/README.md),
[author programme](../detailed-designs/administration/author-programme/README.md),
[author module](../detailed-designs/administration/author-module/README.md),
[author section](../detailed-designs/administration/author-section/README.md),
[order curriculum](../detailed-designs/administration/order-curriculum/README.md), and
[publish curriculum](../detailed-designs/administration/publish-curriculum/README.md).
Visual references: [programme index](../mocks/admin-programmes.html),
[programme editor](../mocks/admin-programme.html) and [its states](../mocks/admin-programme-states.html),
[module editor](../mocks/admin-module.html) and [its states](../mocks/admin-module-states.html),
[section editor](../mocks/admin-section.html), [refused access](../mocks/admin-denied.html),
[authoring on a phone](../mocks/admin-mobile.html), and [state legend](../mocks/state-legend.html).

ATDD starts with an acceptance test expecting eight module markers from an eight-module
import against a validator that demanded twelve. Subsequent acceptance tests cover the
journey above.

## Implementation notes

One migration, `AuthorCurriculum`, expands the schema, backfills a published `Curricula`
row from every existing key (draft where a cohort's key has no modules), and contracts
the key columns, in one `Up` with a symmetric `Down`. The import merge is retired:
`import-curriculum` creates one draft programme and refuses a key already in use.

The authoring screens share the participant shell. The `admin` branch carries
`data: { gated: false }`, the shell reads the deepest such value on every navigation, and
the enrollment gate steps aside, so an unenrolled administrator never sees the not-enrolled
notice on an authoring screen. The preview is a child route of the module editor; the
route reuse strategy keeps a route that asks to be kept while its identifier is unchanged,
and keeps the componentless `admin` branch so its children can be compared one by one,
which is what leaves every unsaved value in the editor beneath the preview. The preview
fetches each section's reading, the one request the module design's "no request" line
did not foresee, because the module draft carries word counts rather than prose.

A draft module requested by ordinal answers 404 rather than a message that would disclose
authored content; the module reader treats that 404 as it treats a locked module. The
sign-in screen names a held authoring route as its path and never resolves a title. The
stacked order control is two 44 × 24 halves, each with its own accessible name, so the
target-size check exempts the pair while holding every other control to 44 × 44.

Authored field maxima are `Curriculum` options carried on every authoring read, counted
in characters as an author counts them. The refused-save state of a stale revision and
the preview, which the mockups do not draw, are designed in code: the conflict aside
offers the current content for comparison and a reload that keeps the typed values,
merging untouched fields to the current content. Two administrators saving at once are
serialised by the programme lock; the concurrency token is the backstop.

## Verification — 2026-09-07

Every gate passes; the figures are recorded in
[`docs/verification/2026-09-07.json`](../verification/2026-09-07.json).

| Gate | Result |
| --- | --- |
| `npm run build` | passed, no warning and no error |
| API acceptance | 74 passed, 0 failed |
| Browser acceptance, desktop and mobile | 126 passed, 0 failed |
| Adapter contract | passed |
| Design system | 2 passed |
| API performance | 33 scenarios within budget, slowest against its budget being sign-in at 145.6 ms of 500 ms |
| Browser transfer and interactivity | 13 screens within 300 KB, the largest 281,811 bytes; curriculum interactive at 1,944 ms of 2,500 ms |

Two checks no suite covers were run by hand. Against the Release build serving its own
`wwwroot`, each of the five authoring routes returned the Angular shell while an
anonymous `GET /administration/curricula` returned 401, which is the SPA deep link the
`/admin` rewrite exists for. The developer database applied both pending migrations with
none left; it stood two migrations back, so that exercised the clean path, and the
backfill of existing keys is covered by the migration acceptance test instead.

One defect surfaced during verification and is fixed. The cohort guard read a programme
the same context had tracked as a draft during an import earlier in the process, so a
cohort created after a publication was refused; `ProgrammeStore.CurriculumByKey` now
reads without tracking, which is what a check-then-act guard under the programme lock
needs.
