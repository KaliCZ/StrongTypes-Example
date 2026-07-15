# ADR-0002 — Validation lives in the type system, not in annotations or guards

**Status:** Accepted (2026-07-15)

## Context

The same input rule ("rating is 1–5", "title is not blank") tends to get
written three times: as a data annotation on the DTO, as a guard clause in the
controller, and again in the business layer because it can be called from
elsewhere. The three copies drift, and none of them changes the *type* — after
the check passes, the value is still a bare `string` or `int` and every layer
below has to trust or re-check it.

This application exists to demonstrate the alternative:
[Kalicz.StrongTypes](https://github.com/KaliCZ/StrongTypes) types that make
the invalid state unrepresentable ("parse, don't validate").

## Decision

Every constrained value is a strong type from the boundary inward, and the
constraint is expressed **nowhere else**:

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
- Rules the type system cannot carry (uniqueness, ownership, cross-entity
  rules) are business logic and follow ADR-0003.

## Consequences

- A rule exists in exactly one place — the type — and holds in the API,
  the domain, the database, and (via ADR-0004) the generated frontend client.
- Method signatures are the documentation: `Task<Result<Review, SubmitReviewError>>
  Handle(ProductId, AuthorId, Rating, NonEmptyString title, …)` states every
  precondition.
- Unit tests for "what if the title is empty" disappear; the case does not
  type-check. Tests that remain are about behavior.
- The cost: contributors must learn the small StrongTypes API surface
  (`Create` throws / `TryCreate` returns null / `As…`–`To…` extensions), and
  LINQ-to-SQL occasionally needs an explicit `.Unwrap()`.
