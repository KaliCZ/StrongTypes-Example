# ADR-0003 — Business failures are enum values in `Result`, not exceptions

**Status:** Accepted (2026-07-15)

## Context

Domain operations can fail for legitimate business reasons: the product does
not exist, the reviewer already reviewed it, you cannot vote on your own
review. Modeling those as exceptions hides them from the compiler — a caller
cannot see from the signature which failures exist, and controllers end up
with `try/catch` ladders that must be kept in sync with the service by hand.

## Decision

- Every domain handler that can fail returns
  `Result<TSuccess, TError>` where `TError` is a **per-operation enum**
  (`SubmitReviewError`, `CastVoteError`, …). Success and error are returned by
  implicit conversion (`return review;` / `return SubmitReviewError.ProductNotFound;`).
- Exceptions are reserved for bugs and infrastructure faults — never for
  business outcomes.
- **Controllers own the HTTP translation.** A controller consumes the result
  with `result.Error is { } error` and maps each enum value in an exhaustive
  `switch` to the proper response: `ValidationProblem` with a field-scoped
  `ModelState` error for input-shaped failures, `NotFound`/`Forbid`/`Conflict`
  where those semantics fit. The domain never sees HTTP.
- Handlers never return domain entities to the controller for serialization;
  they return a success model, and the controller maps it to a response DTO
  (see ADR-0001 for the DTO layer).

## Consequences

- The set of possible failures of an operation is part of its signature; a new
  enum value fails compilation at every non-exhaustive `switch` that maps it.
- No `try/catch` in controllers; no custom exception types in the domain.
- Each error enum value appears in exactly one HTTP mapping, so the API's
  error behavior is reviewable in one file per feature.
- Minor duplication: each feature carries its own small enum instead of a
  shared error catalogue — deliberate, so slices stay independent.
