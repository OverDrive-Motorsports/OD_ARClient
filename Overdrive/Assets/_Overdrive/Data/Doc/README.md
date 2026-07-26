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

All shapes below are confirmed against the backend's own endpoints doc
(the authoritative one — supersedes both the earlier draft and
`gateway/ROUTES.md` wherever they disagreed). **Auth is now mandatory on
every route** (`Authorization: Bearer <token>`, was optional before) — see
[Core/Doc/README.md](../../Core/Doc/README.md).

- `DTO/Health/HealthStatusDTO.cs` mirrors the gateway's `GET /health`
  response — the first DTO in the project.
- `DTO/Championship/` mirrors the "Championnat" section, grouped into 3
  files by lifecycle: `CatalogDTO.cs` (`ChampionshipDTO`, `EventDTO`),
  `SessionDTO.cs` (`SessionDTO`), `ParticipantDTO.cs`
  (`SessionDriverDTO`/`DriverProfileDTO`, `TeamDTO`, `StandingEntryDTO`).
  `EventDTO`/`SessionDTO` used to be two DTOs each (summary vs detail) —
  the backend confirmed both endpoints return the exact same fields, just
  singular vs array, so each is now a single DTO/single mapper overload.
  `SessionDTO` also dropped its nested weather entirely (`weatherAtStart`
  no longer exists on this resource). `DriverProfileDTO` dropped
  `driverPicture` (confirmed: no data source, never returned).
- `Entities/ChampionshipEntities.cs` bundles `Championship`,
  `ChampionshipEvent`, `RaceSession`, `Driver`, `Team` — same grouping
  convention already used by `Mock/ODChampionshipMockData.cs`.
  `Entities/StandingEntry.cs` stays its own file — it's the one entity
  refreshed often, worth being able to spot at a glance in the file list.
- `Mappers/ChampionshipMappers.cs` — one `TryApply` per entity now (Event
  and Session lost their second overload along with the summary/detail
  split). `EntityCollectionSync.cs` stays separate — it's a generic
  utility, not a domain mapper.
- Wired to the backend: `Core/Network/ChampionshipApi.cs` (see
  [Core/Doc/README.md](../../Core/Doc/README.md)). `GetDriverProfile` is
  global (`driverNumber` + optional `championshipCode` filter) — it does
  **not** take a `sessionId`, unlike every other championship endpoint.
  Standings path is `/sessions/{sessionId}/standings` (no `/race` suffix).
- `DTO/Race/` mirrors the "Live - Course" section: `RaceTimingDTO.cs`
  (`RacePositionDTO`, `LapDTO`/`RaceLapsDTO`, `StintDTO`, `PitStopDTO`),
  `RaceControlDTO.cs` (`RaceControlEventDTO`/`PenaltyDTO`),
  `RaceWeatherDTO.cs` (`RaceWeatherSampleDTO`), `TeamRadioDTO.cs`
  (`TeamRadioMessageDTO`) — all fields confirmed unchanged from the
  original draft. Matching Entities in `Entities/RaceEntities.cs`, mapped
  by `Mappers/RaceMappers.cs`. Wired to `Core/Network/RaceDataApi.cs`
  (confirmed paths: `/race/position`, `/race/laps`, `/race/stints`,
  `/race/pitstops`, `/race/control` (`POST`, long-poll — confirmed, see
  Core/Doc), `/race/weather`, `/race/radio`). `position` and `laps` return
  a single object instead of an array when `driverNumber` is passed —
  `RaceDataApi` has both a list method and a single-entity method for each.
- `DTO/Telemetry/TelemetryDTO.cs` mirrors the "Live - Telemetrie" section —
  confirmed much simpler than first modeled: `SpeedSampleDTO`
  (`speed, gear, timestamp`) and `EngineSampleDTO` (`rpm, gear,
  throttlePercent, brakePercent, drsActive, timestamp`), both full sample
  histories (arrays), no `driverNumber` field (the path already scopes
  it). `battery` confirmed **not to exist** on `/telemetry/engine` — removed
  entirely, not just left null. `LocationSampleDTO`/`IntervalsDTO`
  unchanged. Matching Entities in `Entities/TelemetryEntities.cs`, mapped
  by `Mappers/TelemetryMappers.cs`, wired to `Core/Network/RaceDataApi.cs`.
- `DTO/Live/LiveUpdateDTO.cs` + `Mappers/LiveUpdateMapper.cs` (a
  speculative composition guessed before any doc described the live
  transport) were **deleted** — the backend doc now reveals the real
  mechanism is Server-Sent Events (`GET /race/live/stream`), not a
  WebSocket, and its payload shapes don't match what was guessed. See
  [Core/Doc/README.md](../../Core/Doc/README.md) for what's confirmed
  about it and why it isn't wired up yet.
- `Mock/ODMockData.cs` (moved from `OD_UI/Dev/`) still holds the fake data
  used by the current UI/Logic (rankings, drivers, videos, championship
  page...). Not yet replaced by real Entities — no screen consumes backend
  data yet.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
