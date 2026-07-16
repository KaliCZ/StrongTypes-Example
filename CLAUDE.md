# CLAUDE.md

Agent instructions for this repository — and the conventions to copy when this
repo is used as a template for a new project.

## Read the design first

The docs are the spec; code follows them. Before changing an area, read what
governs it:

| Changing…                          | Read first |
| ---------------------------------- | ---------- |
| Anything at all                    | [docs/technical-requirements.md](docs/technical-requirements.md) |
| User-facing behavior               | [docs/business-requirements.md](docs/business-requirements.md) |
| A decision that feels reversible   | The matching ADR in [docs/adr/](docs/adr/README.md) |
| Validation, DTOs, domain types     | [ADR-0002](docs/adr/0002-validation-lives-in-the-type-system.md), [ADR-0003](docs/adr/0003-business-errors-are-result-enums.md) |
| API surface / frontend types       | [ADR-0004](docs/adr/0004-openapi-is-the-frontend-contract.md) |
| Auth                               | [ADR-0005](docs/adr/0005-auth-zitadel-oidc-pkce.md) |
| Tests                              | [ADR-0007](docs/adr/0007-tests-use-real-dependencies.md) |

If a change contradicts a doc, change the doc in the same PR — or don't make
the change.

**ADRs are the current design, never a change history.** Every ADR describes
how the application works *now*. Changing or rolling back a decision means
rewriting the matching ADR to the new thinking, in the same PR — git holds
the history. Never mark an ADR superseded, and never add a new ADR for an
aspect an existing one covers; new numbers are only for new aspects. A
rolled-back approach usually earns a line under "Alternatives considered".

## Commands

```shell
dotnet run --project src/ProductReviews.AppHost   # the whole stack (Docker required)
dotnet build ProductReviews.slnx                  # warnings are errors
dotnet test                                       # unit + property + integration (Docker required)
npm --prefix frontend run test:unit               # Vitest
npm --prefix frontend run test:e2e                # Playwright against the full stack
npm --prefix frontend run typecheck               # vue-tsc
npm --prefix frontend run refresh:api             # refresh openapi.json + regenerate schema.d.ts (stack must be running)
dotnet ef migrations add <Name> --project src/ProductReviews.Api --output-dir Persistence/Migrations
```

Demo sign-in: `demo@productreviews.local` / `ProductReviews123!` (second user:
`critic@productreviews.local`). If Zitadel misbehaves after config changes,
reset it: remove the `productreviews-zitadel` container, the
`productreviews-postgres-data` volume, and the `.zitadel/` directory, then
restart the AppHost.

## Architecture rules (the non-negotiables)

1. **Vertical slices.** A feature lives in `Features/<Feature>/` — one folder,
   everything it owns inside: entities, handlers + error enums, controller,
   DTOs, mapping (ADR-0001). This applies to infrastructure too: one concern
   per file (`Infrastructure/RateLimits.cs`, `Health.cs`, …) with
   `Configure(...)`/`Use(...)` statics. No grab-bag `ServiceExtensions`.
   Entities and handlers still never touch HTTP — `HttpContext`, action
   results, and DTOs stay in the controller.
2. **DDD for data and logic.** Entities own their behavior
   (`Review.ApplyEdit`, `Product.RefreshRatingSummary`); handlers orchestrate,
   never property-spray. One handler file per operation, containing its error
   enum.
3. **Controllers, not minimal APIs.** Endpoints are controller actions with
   explicit `ProducesResponseType` metadata.
4. **Strong types everywhere; never lose information.** Constrained values use
   `Kalicz.StrongTypes` wrappers (or a custom `[NumericWrapper]` like
   `Rating`) in DTOs, handler signatures, entities, and responses. Data
   annotations and guard clauses re-checking a type's rule are defects.
   External input parses with `As…`/`TryCreate` (null = reject); internal
   values use `To…`/`Create` (throw = bug).
5. **DTOs are an API-layer contract.** Domain objects are never serialized.
   Handlers return domain models + error enums; the controller maps success to
   a response DTO and each error enum value to HTTP in an exhaustive `switch`
   (no `default` arm — a new enum value must break the build).
6. **Optionality is nullability, and it round-trips.** Required C# property ⇒
   `required` non-nullable in OpenAPI ⇒ required in TypeScript. Three-state
   updates (keep / clear / set) use `Maybe<T>?`, never a magic value.
7. **Proof-of-loading reads.** Multi-entity reads project into records whose
   constructors demand the loaded data (`ProductWithReviews`,
   `ReviewWithVotes` via `CompleteQueries`) — never pass entities around and
   hope the navigation was included.
8. **Entity Framework: set the navigation property together with the FK.**
   When assigning a relationship by ID (e.g. `ProductId`), also assign the
   navigation property (`Product`) — in constructors, factory methods, and
   setters alike — so that right after the call, `something.Product` is
   usable and non-null. The exception is performance-sensitive work like
   inserting thousands of rows in a batch: there it's fine to set only the ID
   rather than load entities into memory that nothing will read.
9. **Denormalized aggregates are recomputed, never incremented** — always from
   source rows, in the same `SaveChanges`.
10. **Seeding happens at startup** (idempotent, through domain entities), never
    in migrations. Migrations are schema-only and tool-generated — never edit
    them by hand.
11. **Tests use real dependencies.** No mocking libraries. Integration tests
    assert raw JSON from anonymous payloads; property tests generate
    strong-typed values (`Kalicz.StrongTypes.FsCheck`). E2E signs in through
    the real Zitadel form.
12. **Tests never reshape production code.** We use DDD: the domain model is
    designed for the domain, not for testability. If a test needs some
    capability, the test figures out how to achieve it — never add methods,
    parameters, setters, or hooks to production code just so a test can reach
    something. Dependency injection should be enough; otherwise the test
    manipulates state itself (seed through EF, reflection as a last resort).
13. **The OpenAPI document is the frontend contract.** Frontend code only
    calls the API through the generated `openapi-fetch` client. After changing
    any DTO/route: run the stack, `npm --prefix frontend run refresh:api`,
    commit `openapi.json` + `schema.d.ts` together with the API change — the
    E2E contract test fails otherwise.
14. **Nothing blanket-global.** `[Authorize]` per action (no
    `RequireAuthorization()` on the route table), resilience per HTTP client
    (no `ConfigureHttpClientDefaults`), no global route middleware in the
    frontend.

## Naming and style

- UTC instants end in `Utc` (`CreatedAtUtc`). Identifiers are spelled out —
  no abbreviations.
- Private fields are camelCase without an underscore prefix; private
  constants and static readonly fields are PascalCase.
- No nested local functions — write a normal method. Don't reach for local
  functions; extract a private method instead. A one-liner local function can
  occasionally be acceptable, but that's an edge case, not something to aim
  for.
- Comments only for a non-obvious *why*, one sentence; when in doubt, none.
- Correctness analyzer rules are build errors (see [.editorconfig](.editorconfig));
  style rules are suggestions. `TreatWarningsAsErrors` is on.
- Central package management: versions live in
  [Directory.Packages.props](Directory.Packages.props) only.

## Where code goes

| New…                       | Goes to |
| -------------------------- | ------- |
| Endpoint + DTOs            | `src/ProductReviews.Api/Features/<Feature>/` |
| Business operation         | `src/ProductReviews.Api/Features/<Feature>/<Verb><Noun>.cs` (handler + error enum) |
| Entity / value type        | `src/ProductReviews.Api/Features/<Feature>/` |
| EF configuration           | `src/ProductReviews.Api/Persistence/Configurations/` |
| Cross-cutting API concern  | `src/ProductReviews.Api/Infrastructure/<Concern>.cs` |
| Orchestration resource     | `src/ProductReviews.AppHost/` |
| Page / component           | `frontend/src/pages/` / `frontend/src/components/` |
| Domain test                | `tests/ProductReviews.Api.UnitTests/` (prefer a property test) |
| API behavior test          | `tests/ProductReviews.Api.IntegrationTests/` (wire-level) |
