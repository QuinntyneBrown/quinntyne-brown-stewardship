# Support

Thank you for using Stewardship. This document describes where to ask questions
and how quickly to expect an answer.

## How to Get Help

Choose the channel that matches your question.

| I want to…                                      | Use                                                                                                              |
| ----------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Understand how something works                  | [Documentation index](docs/README.md)                                                                            |
| Set the project up locally                      | [Development guide](docs/development.md)                                                                          |
| Ask a question or share an idea                 | [GitHub Discussions](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/discussions)                   |
| Report something that is broken                 | [Open a bug report](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=bug_report.yml) |
| Propose a feature                               | [Open a feature request](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues/new?template=feature_request.yml) |
| Report a security vulnerability                 | **Do not open an issue.** Follow [SECURITY.md](SECURITY.md)                                                       |
| Contribute a change                             | [Contributing guide](CONTRIBUTING.md)                                                                             |

## Before You Ask

Most setup questions are answered by three checks:

1. Your toolchain matches the versions in [Prerequisites](docs/development.md#prerequisites)
   — .NET 10 SDK, Node 22.21 or a compatible newer Node 22, and SQL Server.
2. `ConnectionStrings__Stewardship` points at a reachable database, and the CLI
   `migrate` command has been run against it.
3. `npm run build` has been run since your last frontend change. The API serves
   built Angular assets from its own origin and will otherwise serve stale ones.

Searching [existing issues](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/issues?q=is%3Aissue)
is worth a minute before opening a new one.

## Response Expectations

Stewardship is maintained by a small number of people, primarily outside of
business hours. Issues and discussions are usually triaged within a week.
Security reports are handled on the faster schedule in
[SECURITY.md](SECURITY.md).

There is no commercial support offering and no service-level agreement. The
software is provided under the [MIT License](LICENSE), without warranty.

## Reporting a Good Bug

A report we can act on includes:

* What you expected to happen, and what happened instead.
* The exact commands you ran and their output.
* Your operating system, .NET SDK version (`dotnet --version`), and Node version
  (`node --version`).
* The correlation identifier from the error response, if the API returned one.
  It lets us find the matching server log entry.
