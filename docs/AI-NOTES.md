# AI Notes

## Purpose

This file is the candidate-facing summary of how AI support was used and which decisions it helped sharpen. It is intentionally concise and should read like notes for reviewers, not a raw chat transcript.

## 2026-03-12 Bootstrap planning

### Candidate note

I explicitly chose not to spend assignment time on real authentication. The stronger signal in this case is correctness under concurrency, persistent state, and clear trade-off documentation.

I also chose to log failed scratch attempts with explicit reason codes. For a lottery context, being able to explain rejected actions is worth the extra persistence work.

### What AI was used for

- turning the assignment text into explicit mandatory scope versus optional scope
- shaping an MVP backlog that fits a 4-6 hour implementation window
- identifying early architecture choices that affect the whole solution
- identifying concurrency risks and the tests needed to prove correctness
- deciding where assignment documentation should live in the repository
- pressure-testing persistence modeling choices before implementation starts

### Key decisions taken

- Keep the assignment backend-first and optimize for correctness over polish.
- Use a single ASP.NET Core solution with Blazor for the minimal UI to avoid split-stack overhead.
- Use PostgreSQL as the persistence boundary and Docker Compose for a reproducible local setup.
- Use straight SQL with `Npgsql` instead of Entity Framework for persistence.
- Rely on database constraints plus transactions for concurrency-sensitive rules instead of in-memory locking.
- Record both accepted and rejected scratch attempts in an audit-friendly way, while keeping successful claims as the authoritative invariant table.
- Use a self-declared participant identifier instead of full authentication.
- Keep the backlog small and coherent: bootstrap, persistence, scratch flow/API, UI, tests, and documentation/polish.
- Cache the participant identifier locally for convenience and provide a visible clear-identity or log-out action.
- Use milestones to separate mandatory assignment scope from optional stretch work, instead of carrying a separate priority-label system.
- Use xUnit plus Testcontainers-backed PostgreSQL integration tests instead of relying on fake repositories or in-memory database substitutes.
- Keep the UI intentionally small: a row/column picker, cached identifier, log-out action, and clear result messaging instead of a bigger frontend build-out.

### Why those decisions fit the assignment

- The mandatory requirements are mostly about persistence, fairness, and concurrency safety.
- Docker Compose plus PostgreSQL adds some setup cost but makes the local story cleaner and more convincing for review.
- Straight SQL keeps schema, constraints, initialization, and transactional behavior explicit, which is more valuable here than ORM convenience.
- Blazor is sufficient for a minimal UI while keeping implementation effort focused on the backend.
- Concurrency correctness is the highest-risk part of the assignment, so it deserves the strongest design and testing focus.
- In a lottery context, storing failed attempts and their reasons improves explainability when users dispute outcomes.
- Real PostgreSQL-backed tests are more credible here because the important invariants are enforced by database constraints and transactions.
- Full authentication would consume time without materially improving the core assignment proof points.
- Caching the identifier improves the demo flow, while a clear local sign-out keeps the simplification honest and understandable.
- A compact Blazor UI is enough to demonstrate the end-to-end flow without spending assignment time on a production-style calendar frontend.

### Notes for implementation

- Seed prize allocation up front rather than calculating winners during scratch requests.
- Do not pre-store all 10,000 non-winning cells if the model can derive "empty" cells safely from the configured grid size plus stored prize/scratch state.
- Keep persistence explicit with hand-written SQL instead of adding EF abstraction during the assignment timebox.
- Keep successful claims authoritative, but add append-only audit records for rejected attempts too.
- Start with a simple user identifier approach instead of full authentication.
- Cache that identifier in the browser for convenience and provide a visible way to clear it.
- Keep the MVP UI to a row/column selector plus result panel rather than rendering all 10,000 cells.
- Keep Docker Compose as the primary documented local run path.
- Keep future multi-game support in mind in the persistence model, but track it as separate work.
- Preserve an audit-friendly data trail for prize allocation and scratch events.
- Prioritize integration tests for same-cell and same-user contention.
- Use Testcontainers for the database-backed tests so the suite validates the actual PostgreSQL constraint behavior without depending on a manually prepared local database.
- Keep `docs/DESIGN.md` intentionally concise and within roughly two pages.
- Keep `README.md` for setup and keep all other product/technical notes under `docs/`.

### What I want a reviewer to see

- I made scope cuts intentionally instead of accidentally omitting work.
- I used AI to pressure-test trade-offs and backlog shape, not to avoid design responsibility.
- I kept the design honest by documenting where the solution is intentionally simplified.
- I kept the GitHub tracker aligned with the actual scope split: mandatory work versus optional showcase work.
- I used GitHub issues, PRs, and PR comments as part of the working record, so reviewers can inspect both the code and the decision trail.

## Supporting detail

The fuller planning trail is kept separately in `docs/AGENT-NOTES.md` so `docs/AI-NOTES.md` remains suitable as assignment-facing notes.
