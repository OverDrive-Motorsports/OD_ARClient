# Data

Represents the application's data. Bridges the backend (JSON/API) and objects
usable on the Unity side. Purely data-oriented layer: no business logic, no
dependency on UI or Interaction.

## Structure

```
Data/
├── DTO/       Classes mirroring the JSON exchanged with the backend (to be created as needed)
├── Entities/  Objects usable on the Unity side (Driver, RaceEvent, Standing...)
└── Mock/      Static development/demo data
```

## Current state

- `DTO/Health/HealthStatusDTO.cs` mirrors the gateway's `GET /health`
  response (`{"status":"ok"}`) — the first real DTO in the project, used by
  [Core/Network](../../Core/Doc/README.md)'s `GatewayHealthApi`.
- `Entities/` is still empty: no championship/race data flows through the
  backend yet, only this health probe.
- `Mock/ODMockData.cs` (moved from `OD_UI/Dev/`) holds the fake data
  currently used by UI and Logic to display rankings, drivers, videos, etc.
  Eventually these mocks should be replaced by real `Entities` fed through
  `Core`.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
