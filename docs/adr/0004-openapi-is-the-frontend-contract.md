# ADR-0004 — The OpenAPI document is the frontend contract

**Status:** Accepted (2026-07-15)

## Context

The frontend needs types for every request and response. Hand-written
TypeScript interfaces drift silently from the API, and they cannot carry the
constraints the backend enforces. The backend already knows its exact wire
shape — including the strong-type constraints — because Swashbuckle renders it
into an OpenAPI document.

## Decision

- The API exposes its OpenAPI document via **Swashbuckle** with
  `options.AddStrongTypes()` from `Kalicz.StrongTypes.OpenApi.Swashbuckle`, so
  the schema carries the real constraints: `Email` → `format: email` +
  `maxLength: 254`, `NonEmptyString` → `minLength: 1`, `Positive<int>` →
  `minimum: 0, exclusiveMinimum: true`, our `Rating` → `minimum: 1, maximum: 5`.
- **Nullability is truth.** A property is `nullable`/optional in the schema if
  and only if it is optional in the C# DTO. Required DTO properties are
  non-nullable and `required` in the schema; no information is lost between
  C# and the schema.
- The frontend's client types are **generated** from that document with
  `openapi-typescript` and consumed through `openapi-fetch` — no hand-written
  request/response types anywhere in the frontend.
- The generated file is committed. A test regenerates it against the running
  API and fails on any diff, so contract drift breaks the build instead of
  breaking users.

## Consequences

- One source of truth: change a DTO and the compiler + the drift test walk the
  change all the way into the frontend.
- Frontend code gets compile-time errors for wrong paths, wrong verbs, missing
  parameters, and mis-typed bodies.
- Constraints are visible to API consumers in Swagger UI — the demo's payoff.
- The OpenAPI document must stay accurate; anything that would degrade it
  (untyped `IActionResult`, missing `ProducesResponseType`) is a review
  blocker.
