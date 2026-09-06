# Deployment guide

Stewardship deploys as two independent artifacts: the API, which serves both the
HTTP endpoints and the built Angular application from one origin, and the design
system, which is a static site of its own.

## Build the API artifact

Build the front end first, then publish:

```powershell
npm run build
dotnet publish backend/src/QuinntyneBrownStewardship.Api --configuration Release --output .local/publish
```

`npm run build` mirrors the design tokens into Angular, builds the three libraries
and the application, builds the design system, and copies the browser bundle into
the API's `wwwroot`. Publishing without it produces an artifact with stale or
missing client assets.

## Before starting a deployment

1. **Apply migrations explicitly**, using the CLI against the target SQL Server:

   ```powershell
   dotnet run --project backend/src/QuinntyneBrownStewardship.Cli -- migrate
   ```

   Run migrations as a deployment step rather than granting schema-change
   permissions to the API's runtime account.

2. **Set the configuration below.**
3. **Configure the HTTPS certificate.**

## Configuration

The API and the CLI read the same settings. Use environment variables in a
deployment; the double underscore is the standard .NET separator for a nested key.

| Setting                          | Purpose                                             |
| -------------------------------- | --------------------------------------------------- |
| `ConnectionStrings__Stewardship` | SQL Server connection string.                       |
| `AllowedHosts`                   | Host names the application will answer to.          |
| `HttpsPort`                      | The port used when redirecting plain HTTP to HTTPS. |

Options also control the password hashing work factor, the 30-day idle session
lifetime, and the sign-in limits. Raise the work factor as hardware improves;
existing hashes remain verifiable.

Never commit a connection string, a certificate, or a password. `.gitignore`
already excludes `*.pfx`, `*.pem`, `*.log`, and the `.local/` directory.

## TLS

The supplied local configuration terminates HTTPS in Kestrel. Plain HTTP on the
configured port redirects to HTTPS.

If you terminate TLS at a load balancer or reverse proxy, configure a trusted
proxy and forwarded headers as part of that deployment. Without it, the
application sees the proxy's address rather than the client's, which affects the
per-origin sign-in limit, and it may generate redirects to the wrong scheme.

## Health checks

`GET /health` reports application and database readiness:

| Status | Meaning                                              |
| ------ | ---------------------------------------------------- |
| `200`  | The application is running and the database answers. |
| `503`  | The application is running but not ready to serve.   |

Point your load balancer's readiness probe at this endpoint. A `503` should
remove the instance from rotation rather than restart it, since the usual cause is
a database that is not yet reachable.

## Operating the running system

**Diagnosing an error.** Error responses carry a correlation identifier and no
internal exception detail. The same identifier is written to the log, which is how
an operator joins a user's report to the failure.

**Auditing bookings.** Booking audit records carry the actor, the action, the
time, and the correlation identifier.

**Administration.** The participant application contains no administrator screens.
Cohorts, curriculum, mentors, and availability are managed with the CLI, which
should be run from a trusted host with its own connection string. See the
[CLI reference](development.md#cli-reference).

**Revising a curriculum in production.** Repeating an import preserves identities
and completion records. Retain existing module, section, and prompt identifiers
and order, and append sections rather than deleting recorded work. New sections
reopen derived module completion. Booked availability cannot be moved through an
import.

## Deploying the design system

The design system is an independent static site with no runtime dependency on the
application:

```powershell
npm --prefix design-system run build
```

Publish the contents of `design-system/dist` to the root of its own static-site
origin. Fonts are self-hosted and ship with their OFL licenses, so no third-party
font host needs to be reachable.

## Scaling notes

The application is designed to run as multiple instances behind a load balancer.
Two details make that safe:

- Sign-in attempts are serialized with a SQL application lock, so concurrent
  instances cannot race past the throttling limits.
- Session state lives in the database, not in process memory, so no sticky
  sessions are required.

Before a release, run the performance harnesses described in
[testing.md](testing.md#performance) and compare the results against the recorded
budgets.
