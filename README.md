# nlo-surprise-calendar

Backend case assignment for Nederlandse Loterij: a concurrency-safe surprise calendar with fair prize distribution and persistent scratched-cell state.

## Run

Use Docker Compose as the primary local run path:

```bash
docker compose up --build
```

Application URLs:

- App: `http://localhost:8080`
- Live health: `http://localhost:8080/health/live`
- Readiness health: `http://localhost:8080/health/ready`
- Default game summary: `http://localhost:8080/api/bootstrap/default-game`
- Scratch endpoint: `POST http://localhost:8080/api/games/default-game/scratch`

To stop the stack:

```bash
docker compose down
```

## Review Trail

The implementation process is tracked in GitHub issues, pull requests, and PR comments in addition to the repository docs. Reviewers can inspect that trail if they want to see the decision-making and review loop behind the code.

## Documentation

- [Design notes](docs/DESIGN.md)
- [AI notes](docs/AI-NOTES.md)
- [Agent notes](docs/AGENT-NOTES.md)
