# Curriculum

This directory holds the manuscript for the curriculum the repository bundles. It is not
documentation about a feature; it is the content itself, kept in Markdown because prose is
easier to write and review there than in JSON.

## How it becomes importable

```powershell
npm run curriculum
```

`scripts/build-curriculum.mjs` reads [`starter.md`](starter.md) and writes
`backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json`. An operator
then imports that file:

```powershell
dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- import-curriculum backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json
```

The import creates the programme in draft. It reaches participants only when an
administrator publishes it. Nothing in this directory is read by the running application,
which is what keeps the repository inside `L1-012`: no part of a curriculum may be authored
in source code, in a build artefact, or in a file the application reads at startup.

## The manuscript's shape

The generator infers structure from headings, so the shape matters:

| Markdown | Becomes |
| --- | --- |
| `# NN Title` | a module, ordinal `NN`. Text before the first `##` is its summary. |
| `## Practice · 45–60 minutes` | the effort estimate, and the numbered lines beneath it become practice steps. |
| `## Prepare` | the bulleted lines beneath it become preparation prompts. |
| any other `## Title` | a reading section, in order of appearance. |

Anything not opening with a two-digit ordinal — this file's siblings, the manuscript's own
preamble — is ignored. Identifiers are derived from ordinal position, so a module keeps its
identifier when its prose changes but not when its position does.

Field limits come from `CurriculumOptions` and are enforced on import: 120 characters for a
module or section title, 400 for a summary, practice step or prompt, 60 for an effort
estimate, and 12,000 for a section's reading. Every module needs at least one section and
at least one practice step; prompts are optional.

## Where the content comes from

The bundled curriculum teaches the redemptive framework published by
[FaithTech](https://faithtech.com/): the 4D Cycle of Discover, Discern, Develop and
Demonstrate, preceded by Prepare, with the Co-Creation Cycle of Request, Receive, Review,
Render and Rejoice inside Develop.

The stage definitions, the movements of lament, the postures of Reject, Receive, Reimagine
and Create, the Co-Creation Cycle and the Scripture selections are taken from
[The FaithTech Playbook](https://faithtech.com/playbook) and the
[FaithTech Workbook](https://github.com/FaithTechCreate/workbook), used and adapted under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The teaching prose around them is
this repository's own, and FaithTech does not endorse this software.

The participant demo also cross-checks the cycle names against
[Liturgy](https://github.com/QuinntyneBrown/Liturgy), which demonstrates the four Ds
and the five Rs inside Develop. Liturgy is a reference example; Stewardship does
not implement its project boards or phase-gate engine. These sources were reviewed
on September 7, 2026. The existing five-module manuscript already covers both
cycles, so the demo imports it rather than creating a second curriculum.

Replacing this curriculum with your own is the expected thing to do. Nothing about the
platform depends on its subject, and only its module count reaches the demo, which reads
that count from the document it imports.
