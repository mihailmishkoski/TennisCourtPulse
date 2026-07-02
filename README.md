# TennisCourtPulse — Live Tennis Match Intelligence




https://github.com/user-attachments/assets/40a5dff6-3207-4458-bcb7-4da648aee40d




ASP.NET Core (net8.0) backend that ingests live tennis data from **api-tennis.com**,
stores it in PostgreSQL, and derives real-time analytics (momentum, effort,
win-probability, turning points) plus finished-match summaries for an Angular client.

## Solution layout

```
src/
  CourtPulse.Domain          Entities + enums
  CourtPulse.Application     Analytics engines, mapper, CQRS handlers, DTOs, abstractions
  CourtPulse.Infrastructure  EF Core DbContext + config, external API client, background sync
  CourtPulse.Api             Controllers, Program.cs, DI wiring
tests/
  CourtPulse.Application.Tests   Unit + real-payload integration tests
```

### The analytics engines (pure, unit-tested — `CourtPulse.Application`)

| Engine | What it answers |
|---|---|
| `MomentumCalculationService` | Momentum graph (cumulative) + live surge meter (EWMA) + effort ("who's trying harder") |
| `TurningPointDetector` | The biggest swings / lead changes |
| `WinProbabilityService` | Live win probability from the point→game→set→match model |
| `MatchSummaryService` | Finished-match strengths / weaknesses / headlines |
| `LiveMatchMapper` | Raw `get_livescore` JSON → clean engine inputs (lossy- and doubles-tolerant) |

These have no DB/HTTP dependencies, so they are covered by fast unit tests plus an
integration test that runs them over a real captured payload.

## Running locally

1. **Configure secrets** (never commit these):
   ```bash
   cd src/CourtPulse.Api
   dotnet user-secrets init
   dotnet user-secrets set "TennisApi:ApiKey" "<your-api-tennis-key>"
   dotnet user-secrets set "ConnectionStrings:CourtPulse" "Host=localhost;Port=5432;Database=courtpulse;Username=postgres;Password=postgres"
   ```

2. **Create the database** (PostgreSQL must be running):
   ```bash
   dotnet ef database update \
     --project src/CourtPulse.Infrastructure \
     --startup-project src/CourtPulse.Api
   ```

3. **Run the API** (the background sync starts automatically, polling every 25s):
   ```bash
   dotnet run --project src/CourtPulse.Api
   ```
   Swagger UI is served at the root in Development.

4. **Run the tests**:
   ```bash
   dotnet test tests/CourtPulse.Application.Tests
   ```

## Endpoints

```
GET /api/matches/live                     live + just-finished matches
GET /api/matches/{id}                      match detail + set scores
GET /api/matches/{id}/timeline             sets → games → points
GET /api/matches/{id}/momentum             momentum graph + surge points
GET /api/matches/{id}/statistics           per-player raw statistics
GET /api/matches/{id}/summary              finished-match strengths/weaknesses
GET /api/matches/{id}/win-probability      live win probability
```

## Notes / known approximations

- The `get_livescore` point-by-point feed is **lossy** (games can arrive with no
  points, or missing the deciding point), so momentum is driven by the reliable
  game-level `serve_winner` and enriched by points where available.
- Win-probability currently uses persisted set/game scores; in-game point score
  and current server are not yet persisted, so it assumes start-of-game. Serve
  strength is derived from match statistics (fallback 0.62).
- Adding migrations:
  `dotnet ef migrations add <Name> --project src/CourtPulse.Infrastructure --startup-project src/CourtPulse.Api --output-dir Persistence/Migrations`
