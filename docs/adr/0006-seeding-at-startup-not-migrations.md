# ADR-0006 — Seed data runs at startup, never inside migrations

**Status:** Accepted (2026-07-15)

## Context

The demo must be browsable the moment the stack is up, which means the
database needs products and reviews without anyone clicking. EF Core offers
two tempting places for that data: `HasData` inside the model (which turns
content edits into schema migrations) or hand-written `INSERT`s inside a
migration (which welds demo content to schema history and re-runs it nowhere).
Both make seed data a schema concern, which it is not.

## Decision

- Migrations contain **schema only**.
- Seeding is an explicit, **idempotent** step that runs at API startup in
  Development, after `MigrateAsync()`: if any product exists, the seeder does
  nothing; otherwise it inserts the demo catalog (products, reviews from a
  cast of fictional authors, and votes) through the domain entities — so seed
  data passes the same strong-type invariants as user input.
- Seed reviews are back-dated and pre-voted so sorting by "most helpful" and
  "newest" is meaningfully demonstrable on first run.
- Production (not applicable to this demo, but the template stance): the API
  neither migrates nor seeds at startup; migrations are applied by the deploy
  pipeline.

## Consequences

- `aspire run` on a fresh machine ends in a populated, clickable catalog.
- Changing demo content is a normal code change — no migration churn, no
  model snapshot noise.
- Because seeding constructs real domain objects, it cannot silently insert
  data that violates an invariant the app enforces.
- Integration tests get a clean database per run and call the same seeder when
  a test needs the catalog.
