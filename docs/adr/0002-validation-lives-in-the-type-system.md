# ADR-0002 — Validation lives in the type system and flows end to end via OpenAPI

**Status:** Accepted (2026-07-15, revised 2026-07-16)

## Context

The same input rule ("rating is 1–5", "title is not blank") tends to get
written many times over: as a data annotation on the DTO, as a guard clause in
the controller, again in the business layer, and once more as a hand-written
TypeScript interface that silently drifts from the API. The copies drift, and
none of them changes the *type* — after the check passes, the value is still a
bare `string` or `int` and every layer below has to trust or re-check it.

This application exists to demonstrate the alternative:
[Kalicz.StrongTypes](https://github.com/KaliCZ/StrongTypes) types that make
the invalid state unrepresentable ("parse, don't validate") — and a pipeline
that carries the constraint to every consumer instead of restating it.

## Decision

Every constrained value is a strong type from the boundary inward, the
constraint is expressed **nowhere else**, and every other representation is
**generated** from it:

- Request and response DTOs use `Email`, `NonEmptyString`, `Positive<int>`,
  `NonNegative<int>`, `Rating` (our own `[NumericWrapper]` type), … — never a
  raw primitive with a data annotation. Invalid JSON fails deserialization and
  becomes an automatic 400 `ValidationProblemDetails` before the action runs.
- **No data annotations, no guard clauses.** A `[Range]`, `[Required]`,
  `[EmailAddress]`, `ArgumentException.ThrowIfNullOrEmpty`, or
  `if (x <= 0) throw` re-checking a rule a strong type already carries is a
  defect.
- Business methods and entities take and hold strong types. EF Core stores
  them directly (`.UseStrongTypes()` on the options builder) so a loaded
  entity carries the same guarantees as a parsed request.
- Construction follows the library's intent split: `input.AsNonEmpty()`
  (nullable result) for external input where invalid means "reject", and
  `input.ToNonEmpty()` (throws) for internal values where invalid means "bug".
- The API renders its OpenAPI document via **Swashbuckle** with
  `options.AddStrongTypes()` from `Kalicz.StrongTypes.OpenApi.Swashbuckle`, so
  the schema carries the real constraints: `Email` → `format: email` +
  `maxLength: 254`, `NonEmptyString` → `minLength: 1`, `Positive<int>` →
  `minimum: 0, exclusiveMinimum: true`, our `Rating` → `minimum: 1, maximum: 5`.
- **Nullability is truth.** A property is `nullable`/optional in the schema if
  and only if it is optional in the C# DTO. Required DTO properties are
  non-nullable and `required` in the schema; no information is lost between
  C#, the schema, and TypeScript.
- The frontend's client types are **generated** from that document with
  `openapi-typescript` and consumed through `openapi-fetch` — no hand-written
  request/response types anywhere in the frontend.
- The generated file is committed. A test regenerates it against the running
  API and fails on any diff, so contract drift breaks the build instead of
  breaking users.
- Rules the type system cannot carry (uniqueness, ownership, cross-entity
  rules) are business logic and travel as `Result` error enums (ADR-0001).

## Consequences

- A rule exists in exactly one place — the type — and holds in the API, the
  domain, the database, the OpenAPI document, and the generated frontend
  client: change a DTO and the compiler + the drift test walk the change all
  the way into the frontend.
- Method signatures are the documentation: `Task<Result<Review, SubmitReviewError>>
  Handle(ProductId, AuthorId, Rating, NonEmptyString title, …)` states every
  precondition. Frontend code gets compile-time errors for wrong paths, wrong
  verbs, missing parameters, and mis-typed bodies.
- Unit tests for "what if the title is empty" disappear; the case does not
  type-check. Tests that remain are about behavior.
- Constraints are visible to API consumers in Swagger UI — the demo's payoff.
  The OpenAPI document must stay accurate; anything that would degrade it
  (untyped `IActionResult`, missing `ProducesResponseType`) is a review
  blocker.
- The cost: contributors must learn the small StrongTypes API surface
  (`Create` throws / `TryCreate` returns null / `As…`–`To…` extensions), and
  LINQ-to-SQL occasionally needs an explicit `.Unwrap()`.
