# Assumptions to validate

Decisions made during the initial build where the brief allowed more than one
reading, or where the original reviews platform was deliberately changed.
Each is easy to reverse — flag any that should go the other way.

1. **Vue 3 + Vite SPA, not Nuxt.** The brief mentioned both a Nuxt preference
   and an explicit "Vue 3 + Vite" stack line; the explicit stack line won
   (ADR-0006). A Nuxt rewrite would keep the generated-client contract
   intact.
2. **Zitadel is in.** The stack list dropped only Temporal and Redis, so the
   earlier "Zitadel for auth" instruction stands: writes require a real OIDC
   sign-in (ADR-0003). If the demo should run auth-free, the AppHost and the
   `[Authorize]` attributes are the only touchpoints.
3. **No moderation at all.** Dropping Temporal removed the durable moderation
   workflows; rather than fake the rules synchronously (1/2/5-star reviews
   held for approval, 1-hour edit window), reviews and edits go live
   immediately. The business requirements document this as the intended
   behavior.
4. **Hard delete instead of soft delete.** The original kept deleted reviews
   as an audit trail behind a partial unique index. Without moderation there
   is no audit consumer, so a review deletes for real (votes cascade) and a
   plain unique index enforces one-review-per-product. Same user-visible
   behavior.
5. **Review photos dropped, product photos kept.** No blob storage in the
   stack; product images are plain seeded URLs. "Photos on reviews" is a
   stated non-goal.
6. **Voting on your own review is now rejected by the API** (`OwnReview`
   error). The original only avoided it in seed data and UI; enforcing it
   server-side gave the demo one more meaningful error-enum value.
7. **Reviews gained optional Pros/Cons fields** (not in the original domain)
   so the PATCH edit has genuinely clearable fields — the `Maybe<T>`
   three-state showcase needs a field where "clear" is legal.
8. **`Rating` is a custom `[NumericWrapper]` strong type**, not the original
   `enum : short` — chosen to demonstrate consumer-defined strong types.
9. **Bot protection (Turnstile) dropped** along with rate-limit
   sophistication; a simple fixed-window limiter on writes remains as the
   infra-slice example.
10. **`UpdatedAtUtc` is null until the first edit** (the original set it on
    creation) so "edited" is representable without comparing timestamps.
11. **Demo credentials are documented in the README** and seeded into
    Zitadel automatically — acceptable for a local demo, obviously not a
    production pattern.
12. **Nothing is pushed.** All work is committed locally on the feature
    branch; pushing and opening the PR happens together after validation.
