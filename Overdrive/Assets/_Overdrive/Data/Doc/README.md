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

- `DTO/` and `Entities/` are empty: no backend exchange exists yet in the
  project (see [Core](../../Core/Doc/README.md)).
- `Mock/ODMockData.cs` (moved from `OD_UI/Dev/`) holds the fake data
  currently used by UI and Logic to display rankings, drivers, videos, etc.
  Eventually these mocks should be replaced by real `Entities` fed through
  `Core`.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
