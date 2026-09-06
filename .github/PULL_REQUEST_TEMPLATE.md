<!--
Thank you for contributing to Stewardship.

Please read CONTRIBUTING.md before opening this pull request. Every section below
matters to review; delete none of them, and write "n/a" where a section does not
apply.
-->

## Summary

<!-- What changed, and why. One or two paragraphs. -->

## Related issue

<!-- Closes #123 — or explain why this change needs no issue. -->

## Requirement

<!--
Name the acceptance criteria this change satisfies (for example L2-020), and say
whether docs/specs/ changed. Behaviour changes must move the requirement in the
same pull request.
-->

## Acceptance test

<!--
Name the test that fails without this change and passes with it, and where it
lives. Every behaviour change needs one.
-->

## Type of change

- [ ] Bug fix (does not change documented behaviour)
- [ ] New behaviour (adds to documented behaviour)
- [ ] Breaking change (existing behaviour changes)
- [ ] Documentation
- [ ] Build, tooling, or tests

## Verification

Commands run locally, and their result:

- [ ] `dotnet test backend/QuinntyneBrownStewardship.sln`
- [ ] `npm run test:e2e`
- [ ] `npm run test:adapters`
- [ ] `npm --prefix design-system test`
- [ ] `npm run test:performance:api` and `npm run test:performance:web` (if this
      change could affect payload size, query count, or render work)

<!-- Paste failures or notable output here. -->

## Checklist

- [ ] This pull request addresses one concern.
- [ ] An acceptance test fails without the change and passes with it.
- [ ] No test contains a selector; selectors live in page objects.
- [ ] No architecture test was added — no structure, naming, banned-API, or
      traceability assertions.
- [ ] One file per type; folders and namespaces agree.
- [ ] Controllers stay thin: bind, dispatch through MediatR, return.
- [ ] Frontend services are consumed through an interface and an
      `InjectionToken`, never a concrete implementation.
- [ ] Components sit in the library their knowledge allows — presentational in
      `components`, service-injecting in `domain`, routed pages in the
      application.
- [ ] No hard-coded hex value, dimension, or font stack in a component
      stylesheet; new values were added to `design-system/tokens.css` first.
- [ ] MediatR is still pinned to 12.5.0.
- [ ] `CHANGELOG.md` has an entry under **Unreleased** if a user would notice
      this change.
- [ ] Documentation is updated if setup, operation, or public behaviour changed.
- [ ] Commits are signed off (`git commit -s`) per the Developer Certificate of
      Origin.

## Screenshots

<!-- For user-facing changes, include before and after at a small and a large viewport. -->

## Notes for reviewers

<!-- Anything you want looked at closely, or a decision you were unsure about. -->
