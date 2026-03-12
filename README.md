# nlo-surprise-calendar

Backend case assignment for Nederlandse Loterij: a concurrency-safe surprise calendar with persistent state, a minimal Blazor UI, and PostgreSQL as the source of truth.

## Run

Primary local run path:

```bash
docker compose up --build
```

Main URLs:

- App: `http://localhost:8080`
- Audit log: `http://localhost:8080/audit`
- API docs: `http://localhost:8080/docs`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`
- Readiness: `http://localhost:8080/health/ready`

Main API routes:

- Summary: `GET /api/bootstrap/default-game`
- Grid state: `GET /api/games/default-game/grid-state`
- Scratch: `POST /api/games/default-game/scratch`
- Audit attempts: `GET /api/games/default-game/audit-attempts`

UI flow:

1. Enter a self-declared participant identifier.
2. Optionally cache it in the browser.
3. Select a tile on the homepage grid.
4. Confirm with the scratch button.
5. Review the result panel or audit page.

Stop the stack with:

```bash
docker compose down
```

## Validation

Local validation:

```bash
dotnet restore NloSurpriseCalendar.slnx
dotnet build NloSurpriseCalendar.slnx -c Release
dotnet test NloSurpriseCalendar.slnx -c Release
```

The test suite uses Testcontainers, so Docker must be available.

GitHub Actions mirrors the same restore, build, and test flow on pull requests to `main`.

## Review Trail

The repo includes both implementation docs and the GitHub working trail. Reviewers can inspect issues, PRs, and PR comments if they want the decision history behind the code.

## Docs

- [Design notes](docs/DESIGN.md)
- [AI notes](docs/AI-NOTES.md)
- [Agent notes](docs/AGENT-NOTES.md)
