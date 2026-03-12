# Design Notes

## Goal

Build a backend-first surprise calendar that stays correct under concurrency, persists state across restarts, and includes a minimal but usable web UI.

## Core rules

- The board has 10,000 cells.
- There is exactly 1 jackpot prize.
- There are exactly 100 consolation prizes.
- A participant may scratch exactly 1 cell.
- A cell may be scratched exactly 1 time.

## Technical shape

- Single ASP.NET Core solution.
- Blazor UI hosted in the same app.
- Minimal HTTP endpoints for summary, scratch, audit, docs, and grid state.
- PostgreSQL as the system of record.
- Docker Compose for the standard local run path.

## Persistence and concurrency

- Straight SQL via `Npgsql`, not Entity Framework.
- Startup-managed schema initialization for the MVP.
- Prize allocation is seeded up front.
- Empty cells are derived from board dimensions plus stored winning and scratched positions.
- The scratch flow is transactional.
- Database constraints enforce one scratch per participant and one scratch per cell.
- Successful claims are authoritative.
- Accepted and rejected attempts are also written to an append-only audit trail.

## UI scope

- Self-declared participant identifier instead of full authentication.
- Client-side identifier cache plus explicit local log out.
- Homepage grid with click-to-select and explicit confirm.
- Only scratched cells reveal outcomes; hidden prize positions are not leaked.
- Audit page for reviewer-facing inspectability.

## Timebox choices

Prioritized:

- correctness
- persistence
- explainability
- concurrency safety
- clear docs

Explicitly de-prioritized:

- real authentication
- visual polish beyond readability
- production infrastructure
- larger multi-game expansion
