# ADR-0003 — Auth is Zitadel OIDC + PKCE; author identity is a hash of `sub`

**Status:** Accepted (2026-07-15)

## Context

Writing reviews and voting require a signed-in user (business requirement
"Access"), so the demo needs a real identity provider that runs locally with
zero manual setup. The frontend is a browser SPA (ADR-0006) with no backend of
its own, so it cannot keep a client secret or host a BFF token exchange.

## Decision

- **Zitadel** runs as a container orchestrated by the Aspire AppHost, using
  its own logical database on the shared Postgres server. On first start the
  AppHost provisions — idempotently, via Zitadel's API — an organization,
  a project, a **public PKCE SPA client** (authorization code + PKCE, no
  secret, JWT access tokens), and a demo human user whose credentials the
  README documents.
- The SPA signs in with `oidc-client-ts` using the standard redirect flow to
  Zitadel's hosted login page, and sends the JWT access token as a Bearer
  header on write requests.
- The API validates tokens with plain `JwtBearer` against Zitadel's discovery
  document and JWKS — issuer and audience validated, no shared secrets.
  Authorization is applied **per endpoint** with `[Authorize]` on write
  actions; reads stay anonymous. There is no blanket
  `RequireAuthorization()`.
- **Author identity** is `AuthorId`: a Guid derived from the SHA-256 hash of
  the token's `sub` claim (first 16 bytes). The database stays Guid-keyed
  regardless of the identity provider's id format, and no identity-provider
  key leaks into domain tables. The display name is snapshotted from the
  `name` claim at write time.

## Consequences

- `aspire run` gives a fully working login with no manual Zitadel clicking;
  the demo user works immediately.
- Tokens live in the browser (in-memory via `oidc-client-ts`) — acceptable
  for a demo/template; a production system wanting cookie sessions would add a
  BFF, which this decision deliberately leaves out.
- Swapping the identity provider later only touches the AppHost provisioning
  and the authority URL; domain data is unaffected because of the `sub` hash.
- E2E tests exercise the real login form — auth is never bypassed or faked.
