# AI Notes

## Purpose

This file is the candidate-facing summary of how AI support was used and which decisions it helped sharpen. It is intentionally concise and should read like notes for reviewers, not a raw chat transcript.

## 2026-03-12 Bootstrap planning

### Candidate note

I explicitly chose not to spend assignment time on real authentication. The stronger signal in this case is correctness under concurrency, persistent state, and clear trade-off documentation.

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
- Rely on database constraints plus transactions for concurrency-sensitive rules instead of in-memory locking.
- Use a self-declared participant identifier instead of full authentication.
- Keep the backlog small and coherent: bootstrap, persistence, scratch flow/API, UI, tests, and documentation/polish.
- Cache the participant identifier locally for convenience and provide a visible clear-identity or log-out action.
- Use milestones to separate mandatory assignment scope from optional stretch work, instead of carrying a separate priority-label system.

### Why those decisions fit the assignment

- The mandatory requirements are mostly about persistence, fairness, and concurrency safety.
- Docker Compose plus PostgreSQL adds some setup cost but makes the local story cleaner and more convincing for review.
- Blazor is sufficient for a minimal UI while keeping implementation effort focused on the backend.
- Concurrency correctness is the highest-risk part of the assignment, so it deserves the strongest design and testing focus.
- Full authentication would consume time without materially improving the core assignment proof points.
- Caching the identifier improves the demo flow, while a clear local sign-out keeps the simplification honest and understandable.

### Notes for implementation

- Seed prize allocation up front rather than calculating winners during scratch requests.
- Do not pre-store all 10,000 non-winning cells if the model can derive "empty" cells safely from the configured grid size plus stored prize/scratch state.
- Start with a simple user identifier approach instead of full authentication.
- Cache that identifier in the browser for convenience and provide a visible way to clear it.
- Keep Docker Compose as the primary documented local run path.
- Keep future multi-game support in mind in the persistence model, but track it as separate work.
- Preserve an audit-friendly data trail for prize allocation and scratch events.
- Prioritize integration tests for same-cell and same-user contention.
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
