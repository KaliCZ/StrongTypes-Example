# ADR-0001 — Vertical feature slices: one API project, controllers, `Result` error enums

**Status:** Accepted (2026-07-15, revised 2026-07-16)

## Context

The codebase has two jobs: demonstrate StrongTypes end to end, and serve as a
starter template. Both jobs punish accidental complexity. A classic onion
(`Api` → `Application` → `Domain` → `Infrastructure` + repositories +
MediatR) scatters one feature across four projects and buries the
StrongTypes story under ceremony — and even a two-project API/domain split
keeps every slice half-complete, with the feature folder always missing its
key domain objects.

Within a slice, domain operations can fail for legitimate business reasons:
the product does not exist, the reviewer already reviewed it, you cannot vote
on your own review. Modeling those as exceptions hides them from the
compiler — a caller cannot see from the signature which failures exist, and
controllers end up with `try/catch` ladders kept in sync by hand.

## Decision

One backend project, `ProductReviews.Api` (next to the Aspire AppHost),
organized **by feature, not by layer**:

- **`Features/<Feature>/`** holds the whole slice: entities, handlers with
  their error enums, the controller, request/response DTOs, and the mapping
  between them.
- **`Persistence/`** holds the `DbContext`, entity configurations, migrations,
  and the seeder. EF Core is used directly as a library — no repository
  interfaces wrapping it.
- **`Infrastructure/`** holds one file per cross-cutting concern
  (`Authentication.cs`, `OpenApi.cs`, `RateLimits.cs`, `Observability.cs`,
  `Health.cs`, …), each a static class with `Configure(...)`/`Use(...)` —
  `Program.cs` is a thin orchestrator and there is no grab-bag
  `ServiceCollectionExtensions`.
- **HTTP stays out of the domain by review, not by compiler.** Entities and
  handlers never touch `HttpContext`, action results, or DTOs; those live
  only in controllers.
- **Handlers return `Result<TSuccess, TError>`, never throw for business
  outcomes.** `TError` is a per-operation enum (`SubmitReviewError`,
  `CastVoteError`, …); success and error are returned by implicit conversion
  (`return review;` / `return SubmitReviewError.ProductNotFound;`).
  Exceptions are reserved for bugs and infrastructure faults.
- **Controllers own the HTTP translation.** A controller consumes the result
  with `result.Error is { } error` and maps each enum value in an exhaustive
  `switch` (no `default` arm — a new enum value must break the build) to the
  proper response: `ValidationProblem` with a field-scoped `ModelState` error
  for input-shaped failures, `NotFound`/`Forbid`/`Conflict` where those
  semantics fit.
- **DTOs are a dedicated API-layer contract.** Domain entities and read
  models are never serialized. Handlers return domain models; the controller
  maps success models to response DTOs and error enums to HTTP.
- **Read paths return proof-of-loading models, not entities.** A query
  handler owns a single query method with its `Include` chain and projects
  into an immutable record whose constructor requires every loaded
  relationship (e.g. `ReviewWithVotes`). If it isn't loaded, the
  model cannot be constructed — no lazy-loading surprises, no
  possibly-null navigations downstream.
- **Controllers, not minimal APIs** — controller classes group a feature's
  endpoints, give Swashbuckle first-class metadata, and keep binding,
  auth attributes, and `ProducesResponseType` declarations in one reviewable
  place per feature.
- **No MediatR / no dispatch layer.** Controllers inject concrete handler
  classes. A handler is a class with one public method; DI registration is
  `Infrastructure/DomainServices.cs`.

## Consequences

- A feature is one folder; deleting a feature is deleting one folder plus its
  EF configuration. Review diffs stay feature-local.
- The set of possible failures of an operation is part of its signature, and
  each error enum value appears in exactly one HTTP mapping — the API's error
  behavior is reviewable in one file per feature. No `try/catch` in
  controllers, no custom exception types in the domain.
- "Domain never sees HTTP" needs review attention: no project edge enforces
  it, only the rule that DTO mapping happens exclusively in controllers.
- No runtime dispatch magic: navigation is F12, stack traces are honest, and
  the template stays approachable for readers new to the stack.
- The cost of not abstracting EF: swapping the persistence technology means
  touching domain handlers. Accepted — the demo's point is EF storing strong
  types directly.
- Minor duplication: each feature carries its own small error enum instead of
  a shared error catalogue — deliberate, so slices stay independent.

## Alternatives considered

- **Onion/Clean Architecture with repositories** — rejected: doubles the file
  count per feature and hides the `UseStrongTypes()` EF integration behind
  interfaces, which is the opposite of a showcase.
- **A separate domain project with a compiler-enforced dependency edge** —
  tried first and rolled back: every feature lived in two half-folders, the
  slice never owned its domain objects, and the edge protected a boundary
  that review can hold.
- **Exceptions for business failures** — rejected: invisible in signatures,
  and the compiler cannot force a controller to handle a newly added failure
  the way an exhaustive `switch` over an enum does.
- **Minimal APIs** — rejected: endpoint metadata and per-route filters get
  re-invented per endpoint, and the controller + `ModelState` +
  `ValidationProblem` pipeline is exactly what the StrongTypes ASP.NET
  integration targets.
- **MediatR** — rejected: indirection without benefit at this scale, and an
  extra dependency for a template that wants to stay legible.
