# Data

Represents the application's data. Bridges the backend (JSON/API) and objects
usable on the Unity side. Purely data-oriented layer: no business logic, no
dependency on UI or Interaction.

## Structure

```
Data/
├── DTO/       Classes mirroring the JSON exchanged with the backend, grouped by domain/lifecycle
├── Entities/
│   ├── Static/  Rarely-changing objects (Championship...) — fetched once, rarely re-fetched
│   └── Live/    Objects re-fetched on a timer while relevant (StandingEntry...) — updated in place, never replaced
├── Mappers/   DTO -> Entity conversion (TryApply mutates an existing entity; see "Error handling" below)
└── Mock/      Static development/demo data
```

An Entity's folder (`Static/` vs `Live/`) is decided purely by how often the
screen showing it needs fresh data — not by the endpoint or the backend
service it comes from.

## Error handling

DTOs stay pure mirrors of the JSON — no validation logic in them.
`Core/Network/ApiClient` already handles network failures and unparseable
JSON. What's left for `Mappers/` is validating the *identifying* field an
Entity needs to be usable elsewhere (e.g. `championshipCode`,
`driverNumber`) — if that's missing, `TryApply` returns an error message and
the caller skips that one entry (logs a warning) instead of failing the
whole list. Cosmetic fields (name, gap...) are never validated: missing just
means an empty value on screen.

## Entities are mutated in place, never replaced

Every mapper follows the same shape: `dto.TryApply(existingEntity)` (returns
`null` on success, an error string otherwise) — it never returns a `new`
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
- `Entities/Static/ChampionshipEntities.cs` bundles `Championship`,
  `ChampionshipEvent`, `RaceSession` (+`SessionWeather`), `Driver`, `Team` —
  same grouping convention already used by `Mock/ODChampionshipMockData.cs`.
  `Entities/Live/StandingEntry.cs` stays its own file (the `Static`/`Live`
  folder split is the meaningful boundary, not one-class-per-file).
- `Mappers/ChampionshipMappers.cs` bundles every `TryApply` (one static
  class per entity, some with two overloads since two endpoints feed the
  same entity with different subsets of fields). `EntityCollectionSync.cs`
  stays separate — it's a generic utility, not a domain mapper.
- Not wired to the backend yet: no `Core/Network` code calls into any of
  this — DTO/Entities/Mappers exist ahead of the network layer that will
  feed them (a `ChampionshipApi.cs` façade, one method per endpoint, the
  same shape as `Core/Network/GatewayHealthApi.cs`).
- `Mock/ODMockData.cs` (moved from `OD_UI/Dev/`) still holds the fake data
  used by the current UI/Logic (rankings, drivers, videos, championship
  page...). Not yet replaced by real Entities — no screen consumes backend
  data yet.

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
