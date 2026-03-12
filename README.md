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

To stop the stack:

```bash
docker compose down
```

## Documentation

- [Design notes](docs/DESIGN.md)
- [AI notes](docs/AI-NOTES.md)
- [Agent notes](docs/AGENT-NOTES.md)
