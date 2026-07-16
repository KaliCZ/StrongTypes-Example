# Architecture Decision Records

Short records of the decisions that shape how a specific aspect of this
application is done — the ones a newcomer would otherwise question or
accidentally reverse. Everything routine lives in
[technical-requirements.md](../technical-requirements.md).

Two hard rules:

- **ADRs are not a change history.** The set of ADRs is always the up-to-date
  description of the current design; every file states how the application
  works *now*.
- **When a decision changes, edit its ADR in place** — in the same PR as the
  change — so it reflects the new thinking. Git holds the history; there are
  no "superseded" ADRs, and a changed decision never gets a new number. A new
  number is only for an aspect no existing ADR covers. A rolled-back approach
  usually earns a line under "Alternatives considered".

Format: **Status / Context / Decision / Consequences** (plus
**Alternatives considered** where the rejected option is instructive).

| #    | Decision                                                                                   | Status   |
| ---- | ------------------------------------------------------------------------------------------ | -------- |
| 0001 | [Vertical feature slices in one API project; controllers, no MediatR](0001-vertical-slices-and-project-layout.md) | Accepted |
| 0002 | [Validation lives in the type system, not in annotations or guards](0002-validation-lives-in-the-type-system.md) | Accepted |
| 0003 | [Business failures are enum values in `Result`, not exceptions](0003-business-errors-are-result-enums.md) | Accepted |
| 0004 | [The OpenAPI document is the frontend contract](0004-openapi-is-the-frontend-contract.md)   | Accepted |
| 0005 | [Auth is Zitadel OIDC + PKCE; author identity is a hash of `sub`](0005-auth-zitadel-oidc-pkce.md) | Accepted |
| 0006 | [Seed data runs at startup, never inside migrations](0006-seeding-at-startup-not-migrations.md) | Accepted |
| 0007 | [Tests exercise real dependencies; nothing is mocked](0007-tests-use-real-dependencies.md)  | Accepted |
| 0008 | [The frontend is a Vue 3 + Vite SPA, not a server-rendered app](0008-frontend-vue-spa.md)   | Accepted |
