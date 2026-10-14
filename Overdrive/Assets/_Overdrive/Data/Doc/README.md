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

- `DTO/` mirrors the backend JSON (`ChampionshipDtos`, `RaceDataDtos`). Every
  DTO name ends with `Dto` so `DTO/link.xml` keeps them from IL2CPP stripping.
- `Entities/` holds what the championship panel and replay gallery display
  (`OffSeasonData`, `WeekendData`, `LiveSessionData`, `ReplayData`,
  `SeasonStandings`).
- `Mappers/ODChampionshipMapper` converts DTOs into entities (pure functions).
- `Mock/` — `ODOffSeasonMockData`, `ODWeekendMockData`, `ODLiveSessionMockData`
  and `ODReplayMockData` are ported from OD_Mobile and shown when the backend
  is unreachable and the user picks the demo data. `ODMockData.cs` (moved from
  `OD_UI/Dev/`) still feeds the older widgets.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
