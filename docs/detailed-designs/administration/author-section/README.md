# Author a section

## Overview

A section is the smallest unit of a Stewardship curriculum a participant acts on. It
carries the reading for one part of a module, and it is the thing a participant marks
complete. Every progress figure in the application counts section completions, which makes
the section the one authored object with recorded history attached to it. This feature is
the section editor, and it is where the rule protecting that history is enforced.

**section** — one ordered part of a module, carrying a title and its reading content

**reading content** — the prose a participant reads within a section

**completion record** — row recording that one participant completed one section at one
time

**dependent record** — any row a participant owns that refers to authored content: a
completion record, a note attached to a module, or a note answering a preparation prompt

Sections are created within a module, take a title and reading content, and are placed
last in the module's order (L2-048 criterion 1). A revision reaches a participant on their
next read of a published module. Removing a section closes the gap it leaves, so the
remaining sections keep a contiguous order and a participant never reads a numbering with a
hole in it.

The governing rule is that recorded participant history outranks authoring convenience
(L2-050). A section at least one participant has marked complete is not removable. The
attempt is refused, the refusal names the recorded completion, and the section remains. The
same rule reaches upward: a module containing such a section is not removable either, and a
preparation prompt a participant has answered in a note is not removable. Content a
participant has acted on shall not be deleted out from under the record of that action.

Completion is not the only record a participant leaves. A note carries an optional
`ModuleId` and an optional `PromptId`, and both are foreign keys, so a participant who
opened a module and wrote a note against it without completing a single section has still
made that module undeletable. The rule therefore counts every dependent record and not only
completions: a module with an attached note is refused (L2-050 criterion 5), and so is a
module holding a prompt someone has answered (L2-050 criterion 6). Counting completions
alone would leave those two removals to fail at the database instead of at the rule.

Enforcement is explicit rather than incidental. Every curriculum foreign key is configured
`DeleteBehavior.Restrict`, so the database refuses a cascading delete on its own. The
handler does not rely on that refusal to produce a usable message; it counts the dependent
records first and raises a `ProgrammeException` carrying `409` and a sentence naming what
depends on the content. A removal blocked by history therefore returns a stated reason and
never a constraint failure (L2-050 criterion 7). The database constraint remains as the
backstop that makes the rule true even if a handler is added later that forgets it.

Section content survives republication untouched. Revising the text of a section a
participant has already completed does not reopen their completion, because completion is
recorded against the section identifier rather than against its text. Adding a *new* section
to a completed module does reopen that module, which is designed in
`modules/complete-section` and consumed rather than changed here.

Reading content is the longest thing the API accepts, and it carries the weakest guard
unless one is stated. A participant's note is capped at a configured length and refused with
a message naming the field; a section of reading had no such cap, leaving the request body
limit of the host as the only ceiling. That limit refuses an oversized body at the
transport, before any handler or validator runs, so an author would have met an opaque
refusal naming nothing rather than the field message L2-051 promises. Reading content and
practice steps therefore carry stated maxima of their own, held in options beside
`NoteOptions` and set below the request body limit, so the field-level message is what an
author actually reads (L2-051 criteria 6 and 7).

Saving reports itself. A revision returns the content the administrator already has on
screen, so nothing visibly changes when it succeeds, and an administrator reading by screen
reader would have no evidence either way. The editor writes the outcome into a polite live
region, so a save is perceivable without sight of the form (L2-060). The same region carries
the refusal when a removal is declined, so success and refusal arrive by one route rather
than two.

A section of reading is the longest thing anyone types into Stewardship, so the editor
guards it. An administrator who has changed a field and not saved it is warned before a
navigation or a tab close discards the change, and choosing to remain leaves every unsaved
change in place (L2-063). The participant note editor already carries this guarantee; the
authoring screens carry the same one, through the same route-guard and `beforeunload`
pattern.

The refusal of L2-050 is also shown before it is provoked. Each section row reports how
many participants have completed it, and each module row reports how many notes are
attached to it and to its prompts, so an administrator sees that content is in use without
attempting a removal and reading the error. The counts come from the same dependent records
the removal is checked against, so the display and the rule cannot disagree.

The module a section belongs to is authored in `administration/author-module`. Reordering
sections belongs to `administration/order-curriculum`. When authored content becomes
visible belongs to `administration/publish-curriculum`. How a participant records a
completion belongs to `modules/complete-section`.

## Description

**Web client.**

- **`SectionEditorComponent`** — component in the `domain` library. It calls
  `inject(AUTHORING_SERVICE)`, holds the authored section in a signal, and saves
  revisions. It belongs in `domain` because it injects an `api` contract.
- **`SectionListComponent`** — component in the `domain` library rendering the sections of
  a module with their positions and offering the removal action.
- **`TextAreaFieldComponent`** — presentational component in the `components` library used
  for the reading content. It injects nothing.
- **`ConfirmDialogComponent`** — presentational component in the `components` library over
  a native `dialog`, shown before a removal.
- **`ErrorMessageComponent`** — existing presentational component in the `components`
  library. It renders the refusal returned when a removal is declined.
- **`StatusMessageComponent`** — existing presentational component in the `components`
  library rendering a `role="status"` region. A save reports its outcome through it.
- **`SectionEditorPageComponent.canLeave`** — `CanDeactivateFn` guard on the authoring
  routes, mirroring the guard the note editor already carries. It returns `false` while the
  editor reports unsaved changes, and the page registers a `beforeunload` handler for the
  tab-close case (L2-063).
- **`dirty`** — `computed()` signal on `SectionEditorComponent` comparing the edited values
  with the values last loaded or saved. It is the single source the guard and the save
  button both read, so neither can disagree about whether work is outstanding.
- **`SectionDraftResult`** — `api` result type carrying one authored section.

**API.**

- **`SectionsController`** — controller in `Api/Controllers/Administration` exposing
  `POST /administration/modules/{id}/sections`, `PUT /administration/sections/{id}`, and
  `DELETE /administration/sections/{id}`. It carries the administration policy.
- **`AddSectionCommand`**, **`AddSectionCommandHandler`**, and
  **`AddSectionCommandValidator`** — the creation slice. The handler appends the section at
  the next free ordinal and stamps `CreatedAt` from `ISystemClock`.
- **`ReviseSectionCommand`**, **`ReviseSectionCommandHandler`**, and
  **`ReviseSectionCommandValidator`** — the revision slice over the title and the reading
  content.
- **`RemoveSectionCommand`** and **`RemoveSectionCommandHandler`** — the removal slice. The
  handler counts completion records against the section, refuses when any exist, and
  otherwise removes the section and closes the gap in the module order.
- **`ModuleSection`** — domain entity for one section, owning its ordinal, title, reading
  content, and creation time.
- **`SectionCompletion`** — domain entity recording that one participant completed one
  section. These records are what a section removal is checked against.
- **`Note`** — existing domain entity carrying an optional `ModuleId` and an optional
  `PromptId`, both foreign keys with `DeleteBehavior.Restrict`. A note is a dependent record
  exactly as a completion is, so a module removal is checked against notes as well. Its
  `Revision` concurrency token is the pattern `ModuleSection` follows for L2-065.
- **`ModuleSection.Revision`** — `Guid` configured as a concurrency token. A save carrying a
  stale revision fails rather than overwriting the revision stored since, and the handler
  translates the failure into a `409` the editor can explain.
- **`CurriculumOptions`** — options type bound through `Microsoft.Extensions.Options`,
  mirroring `NoteOptions`. It holds the maximum length of a title, of reading content, and
  of a practice step. `AddSectionCommandValidator` and `ReviseSectionCommandValidator` read
  it, and `TextAreaFieldComponent` reports the same maximum to the author while they type.
- **`ICurriculumStore`** — application abstraction. It supplies `Remove<T>(entity)` and
  the dependency counts the rule reads: `CompletionCount(sectionId, token)`,
  `ModuleNoteCount(moduleId, token)`, and `PromptAnswerCount(moduleId, token)`. Counting
  through the store rather than catching a database exception is what lets the refusal name
  what depends on the content.
- **`ProgrammeException`** — existing application exception carrying an HTTP status code
  and a message. A refused removal raises it with `409`.
- **`OrdinalSequence`** — domain service assigning contiguous positions. This slice calls
  its `Compact` operation, which renumbers the remaining sections from 1 after a removal and
  needs no staging pass. The service is described in `administration/order-curriculum`.

`ModuleSection.CreatedAt` already exists and already matters: the participant progress
calculation filters completion by it so that appending a section does not retroactively
rewrite history. This feature sets it and otherwise leaves that behaviour alone.

## Requirements

The feature realises the following level-2 (L2) requirements. Each L2 requirement refines
a level-1 (L1) requirement, cited by identifier. Requirement text is quoted from
`docs/specs/L2.md` unchanged.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-048` | `L1-012` | Sections carry the reading content of a module and are authored within it. |
| `L2-050` | `L1-012` | Recorded participant history outranks authoring convenience. Content a participant has completed, answered, or written a note against cannot be deleted out from under that record. |
| `L2-063` | `L1-012` | Authored content is long, and a section of reading is the longest of it. An administrator who leaves an authoring screen holding unsaved changes must be warned before those changes are lost. |
| `L2-065` | `L1-012` | Curriculum is shared, and two administrators may hold the same module or section open. A save must not overwrite a revision made since the content was loaded, and a failed save must not cost the administrator their work. |

## Diagrams

### Containers

The section editor writes to the same table a participant reads from, and the completion
records that constrain a removal live beside it. A context view is omitted: the
administrator is the only party to this feature, so the view would restate the container
view with less detail.

![C4 container view for authoring a section](diagrams/c4-container.png)

### Components

`RemoveSectionCommandHandler` is the enforcement point for L2-050, and it reads completion
records before it removes anything. The `DeleteBehavior.Restrict` configuration on the
foreign key stands behind it as a backstop.

![C4 component view for authoring a section](diagrams/c4-component.png)

### Class structure

`ModuleSection` is owned by its module and referenced by every `SectionCompletion` recorded
against it. That reference is what a removal is checked against, and it is why the section
is the object the history rule protects.

![Class diagram for authoring a section](diagrams/class-structure.png)

### Behaviour — add a section

The handler appends the section at the next free ordinal and stamps its creation time, so
the progress calculation can tell new content from old (L2-048 criterion 1).

![Sequence diagram for adding a section](diagrams/sequence-add-section.png)

### Behaviour — refuse a removal

The handler counts completion records before removing anything, and refuses with a message
naming the recorded completion. The section remains and the participant's progress is
unchanged (L2-050 criteria 1 and 4).

![Sequence diagram for refusing a section removal](diagrams/sequence-refuse-removal.png)
