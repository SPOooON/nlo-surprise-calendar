# AI Notes

## Purpose

This file summarizes how AI support was used. It is meant for reviewers, not as a raw transcript.

## How AI was used

- turn the assignment into explicit mandatory vs optional scope
- shape a backlog that fits the 4-6 hour timebox
- pressure-test architecture and persistence choices before implementation
- identify concurrency risks and the highest-value tests
- help keep docs, issues, and PRs aligned with the actual implementation

## Main decisions

- Keep the assignment backend-first and optimize for correctness over polish.
- Use one ASP.NET Core app with Blazor instead of a split frontend/backend stack.
- Use PostgreSQL plus Docker Compose as the standard local setup.
- Use straight SQL with `Npgsql` so schema rules and transaction boundaries stay explicit.
- Treat the database as the final authority for concurrency-sensitive rules.
- Use a self-declared participant identifier instead of full authentication.
- Cache that identifier locally and provide a visible local log-out action.
- Record both successful claims and rejected attempts for auditability.
- Use Testcontainers-backed PostgreSQL tests instead of fake repositories or in-memory substitutes.
- Prefer reviewer-facing optional work first: audit view, docs UI, homepage grid, then CI.

## Why those decisions fit

- The assignment is mainly about persistence, fairness, and concurrency safety.
- Full authentication would consume time without improving the strongest proof points.
- Straight SQL is easier to defend here than ORM abstraction.
- Real PostgreSQL-backed tests are more credible because the key invariants are enforced in the database.
- Readable reviewer-facing extras add more value than larger stretch architecture.

## Candidate notes

- I made scope cuts intentionally instead of accidentally omitting work.
- I used AI to pressure-test trade-offs and backlog shape, not to avoid design responsibility.
- I kept simplifying when usability or readability got worse during review.
- I kept the GitHub tracker and repo docs aligned so the reasoning is inspectable.

## Implementation notes

- Prize allocation is seeded up front.
- Empty cells are derived rather than pre-stored.
- Scratch operations are transactional.
- Successful claims stay authoritative; attempt events stay append-only.
- The homepage grid is backed by persisted board state and only reveals scratched cells.
- CI intentionally mirrors the local restore, build, and test workflow without adding extra pipeline scope.

The fuller planning trail is kept separately in `docs/AGENT-NOTES.md`.
