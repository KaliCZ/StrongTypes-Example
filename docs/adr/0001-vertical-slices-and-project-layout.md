# ADR-0001 — Vertical feature slices in two projects; controllers, no MediatR

**Status:** Superseded by [ADR-0009](0009-single-api-project.md) (2026-07-16)

## Context

The codebase has two jobs: demonstrate StrongTypes end to end, and serve as a
starter template. Both jobs punish accidental complexity. A classic onion
(`Api` → `Application` → `Domain` → `Infrastructure` + repositories +
MediatR) scatters one feature across four projects and buries the
StrongTypes story under ceremony; a single flat project loses the
compiler-enforced rule that HTTP concerns never leak into business code.

## Decision

Two application projects with one compiler-enforced dependency edge, both
organized **by feature, not by layer**:

- **`ProductReviews.Api`** — the HTTP boundary. `Features/<Feature>/` holds
  the controller, its request/response DTOs, and the mapping between DTOs and
  domain models. `Infrastructure/` holds one file per cross-cutting concern
  (`Authentication.cs`, `OpenApi.cs`, `RateLimits.cs`, …), each a static class
  with `Configure(...)`/`Use(...)` — `Program.cs` is a thin orchestrator and
  there is no grab-bag `ServiceCollectionExtensions`.
- **`ProductReviews.Domain`** — everything else. `<Feature>/` folders hold the
  entities and one file per operation (`SubmitReview.cs` = handler class +
  its error enum). `Persistence/` holds the `DbContext`, entity
  configurations, migrations, and the seeder. EF Core is used directly as a
  library — no repository interfaces wrapping it.
- **Controllers, not minimal APIs** — controller classes group a feature's
  endpoints, give Swashbuckle first-class metadata, and keep binding,
  auth attributes, and `ProducesResponseType` declarations in one reviewable
  place per feature.
- **No MediatR / no dispatch layer.** Controllers inject concrete handler
  classes. A handler is a class with one public method; DI registration is a
  per-project `AddDomain()` extension.
- **DTOs are a dedicated API-layer contract.** Domain entities and read
  models are never serialized. Handlers return domain models
  (`Result<Review, SubmitReviewError>`); the controller maps success models
  to response DTOs and error enums to HTTP — both directions live in the API
  slice.
- **Read paths return proof-of-loading models, not entities.** A query
  handler owns a single query method with its `Include` chain and projects
  into an immutable record whose constructor requires every loaded
  relationship (e.g. `ReviewWithViewerContext`). If it isn't loaded, the
  model cannot be constructed — no lazy-loading surprises, no
  possibly-null navigations downstream.

## Consequences

- A feature is one folder in each of two projects; deleting a feature is
  deleting two folders. Review diffs stay feature-local.
- The `Api → Domain` project reference makes "domain never sees HTTP"
  structural; the missing reverse reference makes "HTTP never bypasses the
  domain" reviewable at a glance.
- No runtime dispatch magic: navigation is F12, stack traces are honest, and
  the template stays approachable for readers new to the stack.
- The cost of not abstracting EF: swapping the persistence technology means
  touching domain handlers. Accepted — the demo's point is EF storing strong
  types directly.

## Alternatives considered

- **Onion/Clean Architecture with repositories** — rejected: doubles the file
  count per feature and hides the `UseStrongTypes()` EF integration behind
  interfaces, which is the opposite of a showcase.
- **Minimal APIs** — rejected: endpoint metadata and per-route filters get
  re-invented per endpoint, and the controller + `ModelState` +
  `ValidationProblem` pipeline is exactly what the StrongTypes ASP.NET
  integration targets.
- **MediatR** — rejected: indirection without benefit at this scale, and an
  extra dependency for a template that wants to stay legible.
