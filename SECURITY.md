# Security Policy

The Stewardship project takes the security of its software seriously. This
document explains which releases receive security fixes, how to report a
vulnerability privately, and what the project already does to protect
participant data.

## Supported Versions

Stewardship is pre-1.0. Security fixes are applied to the `main` branch and
released in the next version. There are no long-term support branches.

| Version | Supported          |
| ------- | ------------------ |
| `main`  | :white_check_mark: |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
discussions, or pull requests.**

Report vulnerabilities using either of the following private channels:

1. **GitHub private vulnerability reporting** (preferred) — open the
   [Security Advisories](https://github.com/QuinntyneBrown/quinntyne-brown-stewardship/security/advisories/new)
   page for this repository and submit a report. This keeps the discussion
   private until a fix is published.
2. **Email** — send the report to **quinntynebrown@gmail.com** with
   `SECURITY` in the subject line.

To help us triage quickly, include as much of the following as you can:

* The type of issue (for example: authentication bypass, injection, insecure
  direct object reference, cross-site scripting, cross-site request forgery,
  privilege escalation).
* Full paths of the source files related to the issue.
* The location of the affected source code (tag, branch, commit, or a direct
  URL).
* Any special configuration required to reproduce the issue.
* Step-by-step instructions to reproduce the issue.
* Proof-of-concept or exploit code, if available.
* The impact of the issue, including how an attacker might exploit it.

Reports written in English are handled fastest.

## Response Targets

| Stage                                        | Target                |
| -------------------------------------------- | --------------------- |
| Acknowledgement of your report                | Within 3 business days |
| Initial assessment and severity determination | Within 10 business days |
| Fix or documented mitigation for high or critical severity | Within 30 days |

If a fix will take longer than these targets, we will tell you why and give a
revised estimate.

## Disclosure Policy

We follow coordinated disclosure. We ask that you give us a reasonable
opportunity to publish a fix before disclosing the issue publicly. When the fix
ships we will publish a GitHub Security Advisory that describes the issue and
its impact, and we will credit you by name or handle unless you ask us not to.

## Scope

In scope:

* The .NET API, application, domain, infrastructure, and CLI projects under
  `backend/`.
* The Angular application and its `api`, `components`, and `domain` libraries
  under `frontend/`.
* The design system under `design-system/`.
* Build, provisioning, and administration scripts under `scripts/`.

Out of scope:

* Vulnerabilities in third-party dependencies that have no exploitable path in
  this project. Report those upstream; tell us if we should pin or patch.
* Findings that require a compromised host, a malicious administrator, or
  physical access to the machine.
* Deployment misconfiguration in an installation you control, unless our
  documentation recommends the insecure setting.
* Reports produced solely by an automated scanner with no demonstrated impact.

## Security Model

These properties are implemented and covered by the acceptance test suite.
Understanding them will help you judge whether a finding is a real weakness.

**Credentials.** Passwords are hashed with ASP.NET Core Identity's V3 salted
PBKDF2 at 210,000 iterations. The work factor is configurable through Options.
Passwords are never logged and never echoed by the CLI prompt.

**Sessions.** Session tokens are random 256-bit values. Only their SHA-256
digest is persisted, so a database disclosure does not yield usable tokens.
Cookies are `Secure`, `HttpOnly`, `SameSite=Strict`, persistent, and scoped to a
single browser session record. The idle lifetime defaults to 30 days.

**Sign-in throttling.** Sign-in attempts are serialized with a SQL application
lock so that concurrent API instances cannot race past the limits: ten failed
attempts per normalized email address and 100 attempts per origin within fifteen
minutes. A refused attempt is recorded without extending its own cooling-off
window. Password verification runs outside the lock; the cooldown is re-checked
under the lock, and the attempt and session commit in one transaction.

**Cross-site request forgery.** Every mutating authentication request requires a
freshly issued CSRF token.

**Authorization.** Notes and progress are private to the owning participant and
their assigned mentor. An unrelated identity receives `404`, not `403`, so the
API does not confirm that a resource exists.

**Input validation.** Requests are validated in the application layer with
FluentValidation before a handler runs. Route identifiers are validated rather
than trusted.

**Error handling.** Error responses carry a correlation identifier and no
internal exception detail. The identifier is written to the log so an operator
can join the two.

**Auditing and integrity.** All programme writes are transactional. Booking
audit records carry the actor, the action, the time, and the correlation
identifier. Notes use revisions to detect concurrent edits.

**Transport.** The API redirects plain HTTP to HTTPS. Deployments that terminate
TLS at a proxy must configure a trusted proxy and forwarded headers; see
[docs/deployment.md](docs/deployment.md).

**Schema changes.** Migrations are applied explicitly through the CLI. The
runtime database account does not need schema-change permissions.

## Security Updates

Security fixes are announced through GitHub Security Advisories on this
repository and noted in [CHANGELOG.md](CHANGELOG.md). Watch the repository and
select **Custom → Security alerts** to be notified.
