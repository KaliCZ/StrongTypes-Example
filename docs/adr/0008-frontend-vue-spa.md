# ADR-0008 — The frontend is a Vue 3 + Vite SPA, not a server-rendered app

**Status:** Accepted (2026-07-15)

## Context

The frontend's only job is to demonstrate that strong-type constraints flow
from the C# DTOs through OpenAPI into TypeScript, and to give the demo a
pleasant UI. All logic lives in the API. A server-rendered frontend (Nuxt)
would add a second server runtime, a BFF layer, and SSR concerns — none of
which showcase anything about StrongTypes — while React is simply not the
house preference (Vue reads closer to an MVC view layer).

## Decision

- **Vue 3 + Vite + TypeScript**, single-page app, `<script setup>` SFCs,
  Vue Router for pages, Pinia only for genuinely shared state (auth session).
- The dev server (and `vite preview` in E2E) **proxies `/api/*` to the
  backend**, keeping the browser same-origin — no CORS configuration and no
  API URL in browser code. Aspire injects the proxy target as an environment
  variable.
- All API calls go through the generated `openapi-fetch` client (ADR-0002);
  hand-written `fetch` calls to the API are a review blocker.
- Frontend validation is derived from the generated schema types, not
  re-invented: the UI can pre-check what the schema states (required, min
  length, rating bounds) but the API remains the enforcer.

## Consequences

- One frontend runtime (static assets + a dev/preview server), one place with
  logic (the API) — matching the "frontend sends all requests to the backend
  for logic" rule.
- No SEO/SSR for product pages; irrelevant for a local demo, and a future
  Nuxt migration would keep the generated-client contract intact.
- Auth must be a browser-side OIDC flow (see ADR-0005) since there is no
  server to hold a session; the access token stays in memory, not in
  localStorage.
