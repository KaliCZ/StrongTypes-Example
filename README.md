# StrongTypes Example — Product Reviews

A complete, runnable showcase for
[Kalicz.StrongTypes](https://github.com/KaliCZ/StrongTypes): a product-reviews
platform where every constrained value is a strong type — from the request DTO
through the domain and the database, out into the OpenAPI document, and all the
way into the generated TypeScript client. No data annotations, no guard
clauses, no hand-written frontend types.

It doubles as a **starter template** for full-stack .NET web apps:
ASP.NET Core (controllers) + EF Core/PostgreSQL + .NET Aspire + Zitadel auth +
Vue 3, with spec-driven docs, ADRs, and a no-mocks test pyramid. The
conventions are documented in [CLAUDE.md](CLAUDE.md) and
[docs/](docs/technical-requirements.md).

## Quickstart

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (running)

One command:

```shell
dotnet run --project src/ProductReviews.AppHost
```

(or `aspire run` if you have the [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview)).

The AppHost starts PostgreSQL, Zitadel (identity provider + hosted login), the
API, and the frontend; installs frontend dependencies on first run; applies
migrations; and seeds a browsable catalog. First start pulls container images —
give it a few minutes; subsequent starts are fast.

Where to click:

- **The app: <http://localhost:5173>** — browse the catalog, open a product,
  read reviews.
- **Sign in** to write reviews and vote: `demo@productreviews.local` /
  `ProductReviews123!` (a second account, `critic@productreviews.local`, same
  password, lets you vote on the first account's review — you can never vote
  on your own).
- **Swagger UI: <http://localhost:5173/swagger>** — look at
  `SubmitReviewRequest`: `rating` carries `minimum: 1, maximum: 5`, `title`
  carries `minLength: 1`, `email` on the profile carries `format: email`.
  Nothing in the codebase states those rules except the types themselves.
- **Aspire dashboard** — the URL is printed on startup; logs, traces, and
  resource states live there.

## What to look at (the StrongTypes tour)

| The point | Where |
| --- | --- |
| DTOs with strong types, zero annotations | [ReviewContracts.cs](src/ProductReviews.Api/Features/Reviews/ReviewContracts.cs) |
| `Maybe<T>` three-state PATCH (omit = keep, `{}` = clear, `{"Value": …}` = set) | [EditReviewRequest](src/ProductReviews.Api/Features/Reviews/ReviewContracts.cs), [Review.ApplyEdit](src/ProductReviews.Domain/Reviews/Review.cs), [EditReview.cs](src/ProductReviews.Domain/Reviews/EditReview.cs) |
| `Result<T, TError>` + error enums instead of exceptions | [SubmitReview.cs](src/ProductReviews.Domain/Reviews/SubmitReview.cs) → mapped exhaustively in [ReviewsController.cs](src/ProductReviews.Api/Features/Reviews/ReviewsController.cs) |
| Declaring your **own** strong type (`[NumericWrapper]`) | [Rating.cs](src/ProductReviews.Domain/Reviews/Rating.cs) |
| EF Core storing strong types directly (`UseStrongTypes()`) | [Persistence.cs](src/ProductReviews.Api/Infrastructure/Persistence.cs), [ReviewsDbContext.cs](src/ProductReviews.Domain/Persistence/ReviewsDbContext.cs), the [migration](src/ProductReviews.Domain/Persistence/Migrations/) (plain `varchar`/`int` columns) |
| OpenAPI carrying the real constraints (`AddStrongTypes()`) | [OpenApi.cs](src/ProductReviews.Api/Infrastructure/OpenApi.cs), snapshot in [frontend/openapi.json](frontend/openapi.json) |
| Constraints flowing into TypeScript (generated client) | [schema.d.ts](frontend/src/api/schema.d.ts) (generated), [client.ts](frontend/src/api/client.ts), [editReviewBody.ts](frontend/src/api/editReviewBody.ts) |
| Property-based tests with generated strong-typed values | [RatingTests.cs](tests/ProductReviews.Domain.Tests/RatingTests.cs), [ReviewEditTests.cs](tests/ProductReviews.Domain.Tests/ReviewEditTests.cs) |
| The OpenAPI claims verified against the running API | [OpenApiDocumentTests.cs](tests/ProductReviews.Api.IntegrationTests/OpenApiDocumentTests.cs) |
| Parse-don't-validate at the query boundary (`Positive<int>` paging, `Rating[]` filters) | [ReviewsController.cs](src/ProductReviews.Api/Features/Reviews/ReviewsController.cs), [GetReviewsPage.cs](src/ProductReviews.Domain/Reviews/GetReviewsPage.cs) |

## Tests

```shell
# Backend: domain unit + property tests, then wire-level integration tests
# (Testcontainers spins up a real PostgreSQL — Docker must be running)
dotnet test

# Frontend unit tests (Vitest)
npm --prefix frontend run test:unit

# End-to-end: builds the frontend, boots the WHOLE stack in E2E mode, and
# drives real browsing, sign-in, reviewing, and voting (Playwright)
npm --prefix frontend run test:e2e
```

Nothing is mocked anywhere — see
[ADR-0007](docs/adr/0007-tests-use-real-dependencies.md).

## The spec

The implementation follows the docs, not the other way around:

- [Business requirements](docs/business-requirements.md) — what the app does,
  in product terms.
- [Technical requirements](docs/technical-requirements.md) — architecture,
  stack, conventions.
- [ADRs](docs/adr/README.md) — the pivotal decisions, one aspect each.
- [Assumptions to validate](docs/assumptions.md) — where the brief allowed
  more than one reading.

## Links

- The library: <https://github.com/KaliCZ/StrongTypes>
- The idea, explained: [Zero-Code Validations in Your .NET API](https://www.kalandra.tech/blog/zero-code-validations-in-your-dotnet-api/)
- NuGet: [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes)
