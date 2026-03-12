# Design Notes

## Status

Bootstrap planning document. This file captures the initial design direction and constraints before implementation starts.

## Assignment goals

Build a backend-first surprise calendar that is correct under concurrency, persists state across restarts, and includes a minimal UI.

## Proposed technical shape

- Single ASP.NET Core solution.
- Minimal API or controller endpoints for scratch operations and read models.
- Blazor-based UI hosted in the same application.
- PostgreSQL as the system of record.
- Docker Compose for local orchestration.

## Core domain constraints

- Calendar contains exactly 10,000 cells.
- Exactly 1 jackpot prize exists.
- Exactly 100 consolation prizes exist.
- Each user may scratch exactly one cell.
- Each cell may be scratched exactly once.

## Persistence direction

- Initialize calendar state and prize allocation in PostgreSQL.
- Use straight SQL via `Npgsql`, not Entity Framework, so schema rules, initialization, and transaction boundaries stay explicit.
- Use startup-managed SQL initialization recorded in a `schema_versions` table as the migration strategy for the MVP.
- Avoid storing all 10,000 non-winning cells if the system can derive empty cells safely from game dimensions plus stored winning/scratched positions.
- Use database constraints to protect uniqueness rules.
- Prefer short transactional writes plus uniqueness constraints for scratch operations rather than optimistic retries as the primary correctness mechanism.
- Keep scratch operations transactional.
- Persist enough data to reconstruct current state after restart without relying on in-memory caches.
- Keep auditability explicit: prize allocation and scratch outcomes should be explainable from persisted records in the database.
- Separate authoritative scratch claims from append-only attempt-event auditing so rejected requests can be explained without weakening core invariants.
- Leave room in the model for future multi-game support, but keep that feature out of the MVP.

## Concurrency direction

- Treat the database as the final authority for conflicting writes.
- Favor transactional scratch handling over in-process locking.
- Design acceptance tests around conflict scenarios, not only the happy path.

## UI direction

- Keep the Blazor UI minimal and functional.
- Support entering a self-declared user identifier, selecting a cell, and showing the result.
- Use a pragmatic row-and-column picker instead of trying to render and manage all 10,000 cells in the MVP UI.
- Cache the identifier client-side for convenience and provide a clear "log out" or "clear identity" action that removes it from local storage.
- Avoid UI work that does not improve the demonstration of correctness.
- Prefer reviewer-facing inspectability work, such as an audit log view, before larger optional UI expansions.

## Identity approach

- Do not implement full authentication in the MVP.
- Treat the submitted user identifier as the participant key enforced by the backend.
- Make it explicit in the UI and docs that this is a demo-time simplification, not secure identity verification.
- Optimize for clarity and low implementation cost rather than pretending weak authentication is real security.

## Timebox trade-offs

Prioritize:

- correctness
- explainability
- persistence
- concurrency safety
- clear docs

De-prioritize unless time remains:

- visual polish
- advanced observability
- CI/CD
- deployment automation
