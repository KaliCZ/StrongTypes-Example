# ADR-0005 — Tests exercise real dependencies; nothing is mocked

**Status:** Accepted (2026-07-15)

## Context

Mock-heavy test suites verify that code calls its collaborators, not that the
system works: they encode today's implementation into the tests and go green
while real queries, mappings, converters, and constraints break. This codebase
leans on exactly those integration points — EF value converters for strong
types, LINQ translation, unique indexes, JSON converters — none of which a
mock exercises.

## Decision

- **No mocking libraries.** No Moq, NSubstitute, or hand-rolled fakes of our
  own interfaces.
- **Unit tests** cover code that has no dependencies to fake in the first
  place: entity behavior, invariants, pure logic. Property-based tests via
  FsCheck + `Kalicz.StrongTypes.FsCheck` generate valid strong-typed inputs so
  invariants are checked across hundreds of values, not three examples.
- **Integration tests** boot the real API with `WebApplicationFactory`
  against a real **PostgreSQL in Testcontainers** — real migrations, real
  seeder, real JSON pipeline, real EF translation. Auth uses locally minted
  JWTs against the API's real JwtBearer validation (test-owned signing key),
  because starting Zitadel per test run buys nothing the E2E suite doesn't
  already cover.
- Integration tests assert at the **wire level**: they post anonymous JSON
  objects (not the API's DTO classes) and read raw JSON, so an accidental
  contract rename fails a test instead of silently tracking.
- **Frontend**: Vitest component tests for view logic; Playwright E2E drives
  the full Aspire-orchestrated stack — real Zitadel login included.

## Consequences

- A green suite means the wire format, the SQL, and the constraints actually
  work — the strong-type value converters and OpenAPI claims are tested, not
  assumed.
- Tests need Docker; CI and contributors must have it. Accepted.
- Integration tests are slower than mock tests; the suite stays fast enough by
  sharing one Postgres container per test run and isolating via unique data
  per test, not per-test containers.
