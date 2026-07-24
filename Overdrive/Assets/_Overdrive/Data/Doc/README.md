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

- `DTO/Health/HealthStatusDTO.cs` mirrors the gateway's `GET /health`
  response — the first DTO in the project.
- `DTO/Championship/` mirrors every response shape from the doc's
  "Championnat" section, grouped into 3 files by lifecycle rather than one
  file per DTO: `CatalogDTO.cs` (`ChampionshipDTO`, `EventSummaryDTO`/
  `EventDetailDTO`), `SessionDTO.cs` (`SessionSummaryDTO`/`SessionDetailDTO`
  + `WeatherDTO`), `ParticipantDTO.cs` (`SessionDriverDTO`/`DriverProfileDTO`,
  `TeamDTO`, `StandingEntryDTO`).
- `Entities/ChampionshipEntities.cs` bundles `Championship`,
  `ChampionshipEvent`, `RaceSession` (+`SessionWeather`), `Driver`, `Team` —
  same grouping convention already used by `Mock/ODChampionshipMockData.cs`.
  `Entities/StandingEntry.cs` stays its own file — it's the one entity
  refreshed often, worth being able to spot at a glance in the file list.
- `Mappers/ChampionshipMappers.cs` bundles every `TryApply` (one static
  class per entity, some with two overloads since two endpoints feed the
  same entity with different subsets of fields). `EntityCollectionSync.cs`
  stays separate — it's a generic utility, not a domain mapper.
- Wired to the backend: `Core/Network/ChampionshipApi.cs` (see
  [Core/Doc/README.md](../../Core/Doc/README.md)) covers every DTO/Entity
  above — `DriverProfileDTO` included, though its endpoint needs a
  `sessionId` the other championship endpoints don't require.
- `Mock/ODMockData.cs` (moved from `OD_UI/Dev/`) still holds the fake data
  used by the current UI/Logic (rankings, drivers, videos, championship
  page...). Not yet replaced by real Entities — no screen consumes backend
  data yet.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
