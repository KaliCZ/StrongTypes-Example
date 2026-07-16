# ADR-0009 — One API project owns the domain; a feature folder owns everything

**Status:** Accepted (2026-07-16) — supersedes [ADR-0001](0001-vertical-slices-and-project-layout.md)

## Context

ADR-0001 split the backend into `ProductReviews.Api` and `ProductReviews.Domain`
(plus a `ServiceDefaults` library), using the project reference to enforce
"domain never sees HTTP". In practice the split fought the vertical slices it
was meant to serve: every feature lived in two half-folders — entities and
handlers in one project, controller and DTOs in another — so the slice that
owned a feature was always missing its key domain objects. For a kick-start
template, two extra projects are ceremony that buries the StrongTypes story.

## Decision

One backend project, `ProductReviews.Api`, next to the AppHost:

- **`Features/<Feature>/`** holds the *whole* slice: entities, handlers with
  their error enums, the controller, request/response DTOs, and the mapping
  between them.
- **`Persistence/`** holds the `DbContext`, entity configurations, migrations,
  and the seeder.
- **`Infrastructure/`** holds one file per cross-cutting concern — including
  `Observability.cs` (OpenTelemetry) and `Health.cs` (health checks), which
  absorbed everything `ProductReviews.ServiceDefaults` used to provide. That
  project is gone.

Everything else in ADR-0001 carries forward unchanged: organized by feature
rather than layer, controllers not minimal APIs, no MediatR, DTOs as a
dedicated API-layer contract, proof-of-loading read models.

## Consequences

- A feature is one folder; deleting a feature is deleting one folder plus its
  EF configuration. The slice is complete — nothing about a feature lives at
  arm's length.
- "Domain never sees HTTP" is now a review rule, not a compiler rule: entities
  and handlers must not touch `HttpContext`, action results, or DTOs. The
  DTO mapping still happens only in controllers.
- Unit tests reference the API project directly
  (`tests/ProductReviews.Api.UnitTests`, formerly `ProductReviews.Domain.Tests`).

## Alternatives considered

- **Keeping the two-project split** — rejected: the compiler-enforced edge
  protected a boundary that review can hold, at the cost of scattering every
  slice across projects.
- **Architecture tests to re-enforce the boundary** — rejected: an extra
  harness is exactly the ceremony this ADR removes; the DTO rule plus review
  suffice at this scale.
