# nlo-surprise-calendar

Backend case assignment for Nederlandse Loterij: a concurrency-safe surprise calendar with fair prize distribution and persistent scratched-cell state.

## Run

Use Docker Compose as the primary local run path:

```bash
docker compose up --build
```

Application URLs:

- App: `http://localhost:8080`
- Audit log view: `http://localhost:8080/audit`
- API docs UI: `http://localhost:8080/docs`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`
- Live health: `http://localhost:8080/health/live`
- Readiness health: `http://localhost:8080/health/ready`
- Default game summary: `http://localhost:8080/api/bootstrap/default-game`
- Grid state endpoint: `GET http://localhost:8080/api/games/default-game/grid-state`
- Scratch endpoint: `POST http://localhost:8080/api/games/default-game/scratch`
- Audit log endpoint: `GET http://localhost:8080/api/games/default-game/audit-attempts`

UI flow:

- enter a self-declared participant identifier
- optionally cache it locally in the browser
- clear it again via the visible `Log out` action
- click a tile on the homepage grid to select a cell
- confirm the scratch with the existing submit button
- inspect the result panel for win, loss, duplicate-user, or duplicate-cell outcomes
- inspect the audit log page to review accepted and rejected attempts with their recorded reason codes
- open the API docs directly from the homepage when you want to inspect or exercise the endpoints

To stop the stack:

```bash
docker compose down
```

## Test

Automated tests run from the repository root:

```bash
dotnet test -c Release
```

The integration tests use Testcontainers to start an isolated PostgreSQL instance, so Docker must be available when running the test suite.

## Review Trail

The implementation process is tracked in GitHub issues, pull requests, and PR comments in addition to the repository docs. Reviewers can inspect that trail if they want to see the decision-making and review loop behind the code.

## Documentation

- [Design notes](docs/DESIGN.md)
- [AI notes](docs/AI-NOTES.md)
- [Agent notes](docs/AGENT-NOTES.md)
