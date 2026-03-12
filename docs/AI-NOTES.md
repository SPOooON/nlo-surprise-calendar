# AI Notes

## Purpose

This file is the candidate-facing summary of how AI support was used and which decisions it helped sharpen. It is intentionally concise and should read like notes for reviewers, not a raw chat transcript.

## 2026-03-12 Bootstrap planning

### Candidate note

I explicitly chose not to spend assignment time on real authentication. The stronger signal in this case is correctness under concurrency, persistent state, and clear trade-off documentation.

I also chose to log failed scratch attempts with explicit reason codes. For a lottery context, being able to explain rejected actions is worth the extra persistence work.

I also kept tightening the demo based on what was actually visible while using it, instead of treating the first pass as finished. That included noticing when the page framing was too subtle and pushing for clearer presentation without expanding the core scope.

After the mandatory scope was done, I prioritized reviewer visibility extras first. That is why the first stretch UI is an audit log view over more decorative or broader product features.
I also caught a CSS regression during the OpenAPI work instead of hand-waving it away, and I chose to simplify the site colors when readability suffered. That was the right trade-off for an assignment demo: plain and legible beats decorative styling.

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
- Split broad optional backlog items into specific GitHub issues once the mandatory scope is done, so stretch work stays reviewable and easy to prioritize.
- Remove optional backlog items again when they are only plumbing and do not stand on their own as useful reviewer-facing scope.
- Add a reviewer-facing audit log view on top of the persisted attempt trail before taking on broader optional features like a live grid or multi-game support.
- For the API docs stretch goal, use built-in ASP.NET Core OpenAPI generation plus Scalar instead of Swagger-specific tooling.
- Add a lightweight regression test when a concrete UI integration issue is found, even if the assignment does not justify a full browser-test stack.

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
- Once the mandatory scope is complete, the best next extras are the ones that improve reviewer visibility with the least architectural churn.
- The audit trail already existed in the data model, so exposing it as a simple read-only view is a high-signal stretch goal with low implementation risk.
- That same rule also means dropping optional issues that are better folded into a stronger parent feature instead of preserving them as standalone backlog noise.
- Built-in OpenAPI plus a homepage-linked docs UI improves reviewer visibility with low implementation risk and better matches modern .NET direction than adding Swagger.
- When presentation gets in the way of clarity, simplify it. The assignment benefits more from readable UI than from visual styling.

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
- Prefer optional work in this order: reviewer visibility first, then inspectability, then bigger architectural expansion.
- Keep optional reviewer views read-only and grounded in persisted data rather than building demo-only mock state.
- If interactive API docs are added, make them reachable from the homepage instead of leaving them as an undocumented route.
- Keep optional reviewer views read-only and grounded in persisted data rather than building demo-only mock state.

### What I want a reviewer to see

- I made scope cuts intentionally instead of accidentally omitting work.
- I used AI to pressure-test trade-offs and backlog shape, not to avoid design responsibility.
- I kept the design honest by documenting where the solution is intentionally simplified.
- I kept the GitHub tracker aligned with the actual scope split: mandatory work versus optional showcase work.
- I used GitHub issues, PRs, and PR comments as part of the working record, so reviewers can inspect both the code and the decision trail.

## Supporting detail

The fuller planning trail is kept separately in `docs/AGENT-NOTES.md` so `docs/AI-NOTES.md` remains suitable as assignment-facing notes.
