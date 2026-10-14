# Data

Represents the application's data. Bridges the backend (JSON/API) and objects
usable on the Unity side. Purely data-oriented layer: no business logic, no
dependency on UI or Interaction.

## Structure

```
Data/
├── DTO/       Classes mirroring the JSON exchanged with the backend, grouped by domain/lifecycle
├── Entities/  Objects usable on the Unity side, mutated in place (no Static/Live subfolders - see below)
├── Errors/    DataError - centralized error type + severity policy for the whole backend pipeline (see "Error handling" below)
├── Mappers/   DTO -> Entity conversion (TryApply mutates an existing entity; see "Error handling" below)
└── Mock/      Static development/demo data
```

`Data/` has its own `Overdrive.Data.asmdef` — needed so
`Tests/EditMode/Overdrive.Data.Tests.asmdef` can reference it (a custom
test assembly can't see the default `Assembly-CSharp` scripts, only other
assembly definitions). A handful of small NUnit tests cover the trickiest
logic — mapper validation (`ChampionshipMapperTests`,
`ChampionshipEventMapperTests`, `DriverMapperTests`,
`StandingEntryMapperTests`) and the add/update/remove reconciliation
(`EntityCollectionSyncTests`) — not exhaustive across every mapper, since
Race/Telemetry mappers follow the exact same pattern already covered here.

Entities aren't split into subfolders by refresh frequency — how often a
screen re-fetches an Entity is a Logic-level decision (see
[Logic/Doc/UI-Refresh-Pattern.md](../../Logic/Doc/UI-Refresh-Pattern.md)),
not something the folder structure needs to encode. Each Entity's header
comment says whether it's expected to be fetched once or refreshed often.

## Error handling

Every backend-communication failure — network, JSON parsing, mapper
validation — is reported as one `DataError` (`Errors/DataError.cs`) instead
of each layer logging its own ad-hoc string:

- `Core/Network/ApiClient` produces `DataErrorKind.Network` (request
  unreachable/non-2xx) or `DataErrorKind.Deserialize` (JSON doesn't parse).
- `Mappers/` produce `DataErrorKind.Validation` when an Entity's
  *identifying* field is missing/inconsistent (e.g. `championshipCode`,
  `driverNumber`) — cosmetic fields (name, gap...) are never validated.
- `DataError.Report()` is the single place deciding severity: `Validation`
  logs a warning (one entry skipped, the rest of the batch is still fine),
  `Network`/`Deserialize` log an error (the whole call failed). Every
  `onError` callback in the project should just call `error.Report()`
  rather than reimplementing this decision.

See [Error-Messages.md](./Error-Messages.md) for the full catalog of
messages this can produce, where the `[Source]` tag comes from, and how to
add a new one.

## Entities are mutated in place, never replaced

Every mapper follows the same shape: `dto.TryApply(existingEntity)` (returns
`null` on success, a `DataError?` otherwise) — it never returns a `new`
entity. The caller owns one long-lived instance (or `List<T>`) per screen and
passes it into every fetch; for lists, `Mappers/EntityCollectionSync.cs`
reconciles it in place (update matched keys, add new ones, drop missing
ones) instead of the list itself being replaced. See
[Logic/Doc/UI-Refresh-Pattern.md](../../Logic/Doc/UI-Refresh-Pattern.md) for
how a screen should hold and refresh these.

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
