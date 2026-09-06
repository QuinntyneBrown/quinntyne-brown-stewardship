# Stewardship Design System

The Stewardship design system is a deliverable in its own right, not a folder
inside the front end. It has its own `package.json`, its own tests, and its own
build; it deploys as its own static site; and it carries **no runtime dependency
on the application**.

It owns the design tokens — colour, spacing, type scale, radius — and that copy is
authoritative. The front end mirrors them.

## Quick start

```powershell
npm --prefix design-system ci
npm --prefix design-system run build
npm --prefix design-system start
```

The catalogue is served on **http://localhost:4318**.

## Layout

| Path            | Contents                                                                               |
| --------------- | -------------------------------------------------------------------------------------- |
| `tokens.css`    | The authoritative design tokens. Everything else follows from here.                    |
| `base.css`      | Base element styles built on the tokens.                                               |
| `fonts.css`     | `@font-face` declarations for the self-hosted families.                                |
| `fonts/`        | Karla and Newsreader, with their OFL licenses and [provenance notes](fonts/README.md). |
| `catalogue.css` | Styles for the catalogue site itself, not for consumers.                               |
| `index.html`    | The catalogue.                                                                         |
| `build.mjs`     | Produces `dist/`.                                                                      |
| `serve.mjs`     | Serves `dist/` for local review.                                                       |
| `tests/`        | Playwright suite, including automated accessibility checks.                            |

## Tokens

Tokens are CSS custom properties under the `--qbs-` prefix, defined on `:root` in
`tokens.css` and re-declared inside a `@media (width < 48rem)` block where a value
changes for small viewports.

| Group           | Examples                                                                                                                                            |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Colour          | `--qbs-page`, `--qbs-surface`, `--qbs-text`, `--qbs-body`, `--qbs-muted`, `--qbs-line`, `--qbs-accent`, `--qbs-on-accent`, `--qbs-error`            |
| Typography      | `--qbs-font-body`, `--qbs-font-heading`, `--qbs-type-body`, `--qbs-type-title`, `--qbs-leading-body`, `--qbs-weight-strong`, `--qbs-tracking-label` |
| Spacing         | `--qbs-space-xs` through `--qbs-space-3xl`, `--qbs-page-gutter`, `--qbs-layout-gap`                                                                 |
| Measure         | `--qbs-reading-max`, `--qbs-form-max`, `--qbs-content-max`, `--qbs-column-min`                                                                      |
| Controls        | `--qbs-control-min`, `--qbs-radius`, `--qbs-border-width`, `--qbs-focus-width`, `--qbs-focus-offset`                                                |
| Layout switches | `--qbs-main-track`, `--qbs-side-track`, `--qbs-nav-display`, `--qbs-menu-display`, `--qbs-path-columns`                                             |

The layout switches are what let the responsive shell change at one breakpoint,
declared once, rather than being restated in every component stylesheet.

### Using a token

Component stylesheets read tokens and nothing else:

```css
.session-card {
  background: var(--qbs-surface);
  border: var(--qbs-border-width) solid var(--qbs-line);
  border-radius: var(--qbs-radius);
  padding: var(--qbs-space-lg);
  color: var(--qbs-body);
  font-family: var(--qbs-font-body);
}
```

> **A hard-coded hex value, dimension, or font stack in a component stylesheet is
> a defect.** Add the missing token here first, name it for its role rather than
> its value, then read it as `var(--qbs-<role>)`.

Name tokens for what they are for. `--qbs-accent` survives a change of brand
colour; `--qbs-gold` does not.

### Mirroring into Angular

`tokens.css` and `base.css` are authoritative. The front end receives a generated
copy:

```powershell
npm run tokens
```

`npm run build` at the repository root runs this automatically, so the application
can never drift from the design system. Edit the copy under `frontend/` and your
change will be overwritten on the next build — edit `design-system/tokens.css`.

## Fonts

Karla and Newsreader are self-hosted under the SIL Open Font License, whose text
ships alongside them. No third-party font host needs to be reachable at runtime.

Newsreader is instanced to the 300–600 weight range at its default optical size of
18, matching the design system's light, normal, and strong tokens, with all 224
source Unicode mappings and OpenType layout features retained. That reduces the
asset from 132,000 to 50,548 bytes. [`fonts/README.md`](fonts/README.md) records
the exact FontTools commands and the glyph-level comparison that verifies the
optimization.

## Testing

```powershell
npm --prefix design-system run build
npm --prefix design-system test
```

The Playwright suite runs against the built catalogue on port 4318 and includes
automated accessibility checks through `@axe-core/playwright`. It never loads the
application.

## Publishing

```powershell
npm --prefix design-system run build
```

Publish the contents of `dist/` to the root of its own static-site origin. It is a
plain static site with no server-side requirements.

## Contributing

Follow the repository's [contributing guide](../CONTRIBUTING.md). For this package
specifically:

- Add the token before the component that needs it.
- Name tokens by role, not by value.
- Keep the catalogue in step: a new token or component belongs in `index.html` so
  it can be reviewed visually and covered by the accessibility checks.
- The design system may not import from the application, and never gains a runtime
  dependency on it.
