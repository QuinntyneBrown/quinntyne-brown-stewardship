# Stewardship documentation

Everything written down about this project, indexed. Start with the
[root README](../README.md) if you have not read it.

## Guides

| Guide                                       | Read it when you want to…                                                        |
| ------------------------------------------- | -------------------------------------------------------------------------------- |
| [Development](development.md)               | Run the application locally, provision a participant, or administer a programme. |
| [Architecture](architecture.md)             | Understand how the system is put together and why.                               |
| [Testing](testing.md)                       | Run the suites, or write a test that fits the project's approach.                |
| [Deployment](deployment.md)                 | Build the artifacts and operate the system in production.                        |
| [Design system](../design-system/README.md) | Work with design tokens, or build and publish the catalogue.                     |
| [Live walkthrough](live-demo.md)            | Reproduce the five-minute video and inspect its live verification evidence.      |

## Contributing and policy

| Document                                    | Purpose                                                       |
| ------------------------------------------- | ------------------------------------------------------------- |
| [CONTRIBUTING.md](../CONTRIBUTING.md)       | How to propose, build, and submit a change.                   |
| [AGENTS.md](../AGENTS.md)                   | The authoritative architectural rules, for humans and agents. |
| [GOVERNANCE.md](../GOVERNANCE.md)           | Who decides what, and how.                                    |
| [SECURITY.md](../SECURITY.md)               | The security model and how to report a vulnerability.         |
| [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) | Expected conduct in project spaces.                           |
| [SUPPORT.md](../SUPPORT.md)                 | Where to ask a question.                                      |
| [CHANGELOG.md](../CHANGELOG.md)             | What has changed.                                             |

## Requirements

Requirements come before code in this project. They are the source the acceptance
tests are written against.

| Document                                                         | Contents                                                                            |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| [specs/L1.md](specs/L1.md)                                       | Fourteen high-level requirements covering the participant and authoring experience. |
| [specs/L2.md](specs/L2.md)                                       | Sixty-two detailed requirements with acceptance criteria.                           |
| [slices/programme-completion.md](slices/programme-completion.md) | Acceptance slices for the participant programme.                                    |
| [slices/awaiting-enrollment.md](slices/awaiting-enrollment.md)   | Acceptance slices for a participant with no cohort.                                 |
| [slices/requirements-audit.md](slices/requirements-audit.md)     | Coverage of requirements by slice.                                                  |

Curriculum authoring is in scope and is specified by `L1-011` through `L1-014`. The
remaining administrator tasks — managing cohorts, enrolling participants, setting mentor
availability, provisioning mentors — stay out of scope for this requirement set and are
served by the CLI.

## Detailed designs

Per-feature designs under [`detailed-designs/`](detailed-designs/). Each carries a
README and rendered PlantUML: C4 container and component diagrams, a class
structure, and one sequence diagram per significant path.

| Subsystem                                          | Features                                                                                                                   |
| -------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| [access](detailed-designs/access/)                 | `sign-in`, `maintain-session`, `guard-routes`                                                                              |
| [enrollment](detailed-designs/enrollment/)         | `resolve-cohort`                                                                                                           |
| [curriculum](detailed-designs/curriculum/)         | `view-path`, `unlock-and-resume`                                                                                           |
| [modules](detailed-designs/modules/)               | `read-module`, `complete-section`                                                                                          |
| [sessions](detailed-designs/sessions/)             | `view-availability`, `book-session`, `change-booking`, `review-session-history`                                            |
| [notes](detailed-designs/notes/)                   | `write-note`, `prepare-for-session`                                                                                        |
| [administration](detailed-designs/administration/) | `authorise-administrator`, `author-programme`, `author-module`, `author-section`, `order-curriculum`, `publish-curriculum` |
| [platform](detailed-designs/platform/)             | `secure-boundary`, `responsive-shell`, `accessible-interaction`, `operate-and-observe`                                     |

## Content and reference

| Path                                           | Contents                                                                       |
| ---------------------------------------------- | ------------------------------------------------------------------------------ |
| [curriculum/starter.md](curriculum/starter.md) | The bundled twelve-module starter curriculum.                                  |
| [mocks/](mocks/)                               | Static HTML mockups of every screen and state, rendered before implementation. |
| [verification/](verification/)                 | Dated verification records from full suite runs.                               |
| [prompt.md](prompt.md)                         | The original brief the project was built from.                                 |
