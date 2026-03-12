# Agent Notes

## Purpose

This file captures planning discussions with the coding agent so the reasoning is visible in the repository and does not depend on chat history.

## 2026-03-12 Bootstrap Backlog Discussion

### User request summary

Start with the bootstrap issue: derive a pragmatic implementation backlog from the assignment, reason explicitly before making changes, do not begin implementation yet, keep the backlog suitable for a 4-6 hour assignment, and keep all documentation except `README.md` under `docs/`.

The user also asked that discussion with the agent be recorded in `AI-NOTES.md` to demonstrate deliberate use of AI support.

Later in the same discussion, the user requested adding a repository `.gitignore` as part of the bootstrap work.

### Working assumptions

- The assignment is backend-first.
- Mandatory stack preferences are `.NET`, `ASP.NET Core`, `Blazor`, `Docker Compose`, and `PostgreSQL` unless a strong reason appears to change them.
- The current repository is in bootstrap state and contains documentation only.
- The goal of this issue is planning and backlog creation, not implementation.

### Scope reasoning

#### Mandatory scope

- A persistent 100x100 surprise calendar with 10,000 cells.
- Exactly 1 jackpot prize and exactly 100 consolation prizes.
- A user may scratch exactly 1 cell.
- A cell may be scratched exactly 1 time.
- State must survive restarts.
- The solution must behave correctly under concurrent requests.
- A minimal web UI must exist.
- The repository must include run instructions and design / AI usage documentation.

#### Optional scope

- Authentication beyond a simple user identifier strategy.
- A polished UI or rich animations.
- CI/CD beyond local validation.
- Production hosting and infrastructure automation.
- Advanced observability beyond basic health checks and logs.

#### MVP vs stretch

MVP should prove correctness:

- ASP.NET Core application with scratch endpoint and minimal Blazor UI.
- PostgreSQL-backed state.
- Deterministic database initialization for prize distribution.
- Concurrency-safe scratch transaction.
- Automated tests for core rules and concurrency-sensitive paths.
- Docker Compose for running app plus database locally.
- `README.md`, `docs/DESIGN.md`, and this file.

Stretch should remain clearly optional:

- CI pipeline.
- Extra health/diagnostic endpoints.
- Better UX polish for the Blazor interface.
- Additional admin/debug views.

### Early design decisions that affect everything

- Prize allocation strategy: pre-seed all winning cells during initialization rather than calculate them dynamically during scratch requests.
- User identity strategy: start with an explicit self-declared user identifier passed from the UI/API to avoid spending assignment time on auth. Cache it client-side for convenience and provide a clear local "log out" action that removes the cached value.
- Concurrency control strategy: enforce uniqueness in the database and perform the scratch flow in a transaction so correctness does not rely on in-memory locks.
- Application shape: a single ASP.NET Core solution hosting API and Blazor UI keeps the timebox realistic.

### Docker Compose and PostgreSQL impact

- Docker Compose adds setup work but removes local environment ambiguity, which is valuable in an assignment.
- PostgreSQL is the right place to enforce uniqueness constraints for "one scratch per user" and "one scratch per cell".
- The backlog must include database initialization, migrations or schema setup, and seed behavior because persistence is not optional.
- Compose should stay minimal: one app service and one PostgreSQL service.

### Blazor impact

- Blazor satisfies the "minimal web UI" requirement without adding a separate frontend stack.
- The UI backlog should stay intentionally small: render the grid, submit one scratch, and show the result.
- Shared .NET types between UI and backend can reduce time and complexity, which fits the assignment better than a split SPA architecture.

### Concurrency risk analysis

Main risk areas:

- Two requests scratching the same cell at nearly the same time.
- The same user attempting multiple scratches concurrently.
- Prize counts becoming inconsistent if the scratch flow is not atomic.
- Race conditions between application checks and persistence writes.

Mitigation direction:

- Use database constraints plus transactional updates as the correctness boundary.
- Treat application-level checks as helpful, but not authoritative.
- Add explicit tests for duplicate cell and duplicate user races.

### Testing strategy

Tests worth prioritizing in a 4-6 hour window:

- Domain tests for prize distribution and rule validation.
- Integration tests for scratch flow success and failure paths.
- Concurrency-focused tests for same-cell and same-user contention.
- Minimal UI smoke coverage only if time remains after backend correctness tests.

Tests that are lower priority for the timebox:

- Exhaustive UI interaction coverage.
- Performance benchmarking beyond basic confidence.
- Full end-to-end browser automation unless it is very cheap to add.

### Required documentation and location

- `README.md`: assignment summary, stack, and local run instructions.
- `docs/DESIGN.md`: architecture, persistence strategy, concurrency approach, and trade-offs.
- `docs/AI-NOTES.md`: AI-assisted reasoning, prompts, decisions, and backlog derivation trail.

Any additional product or technical docs should also live under `docs/`.

### Backlog shaping rationale

The backlog should stay small and coherent. A pragmatic split for this assignment is:

- bootstrap and solution skeleton
- persistence and schema foundation
- concurrency-safe scratch flow with API
- minimal Blazor UI
- test coverage for rules and concurrency
- documentation and final polish

This keeps the backlog reviewable without fragmenting the work into administrative noise.

### Bootstrap hygiene note

Adding a minimal `.gitignore` is justified during bootstrap because the repository already produced local `.NET` state (`.dotnet/`) while validating commands. Ignoring common local and build artifacts keeps the planning branch clean without affecting product scope.
