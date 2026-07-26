# Core

Central point for backend communication: HTTP API, WebSocket, OAuth, and more
broadly any global service (runtime configuration, session...).

Contains **no business logic related to gameplay or UI**: Core knows how to
talk to the server and expose data/events, but never decides what the
application does with that data (that's [Logic](../../Logic/Doc/README.md)'s job).

## Current state

`Network/` holds every real network call so far: a gateway health check,
the championship endpoints, and the race/telemetry endpoints. Nothing
displayed in the app uses this yet though — screens still read from static
mocks (see [Data/Mock](../../Data/Doc/README.md)); wiring a screen to
`ChampionshipApi`/`RaceDataApi` is the next step.

```
Core/
├── Network/
│   ├── ApiConfig.cs            Gateway base URL + Bearer token (now mandatory - see below)
│   ├── ApiClient.cs            Generic coroutine GET/POST, deserializes with Newtonsoft
│   ├── GatewayHealthApi.cs     GET /health
│   ├── GatewayHealthCheck.cs   Manual test MonoBehaviour (not wired into app startup)
│   ├── ChampionshipApi.cs      Championship endpoints (see below)
│   └── RaceDataApi.cs          Race/telemetry endpoints (see below)
├── Auth/      OAuth, session/token management (not started)
└── Services/  Global services (SceneLoader, AppSettings...) (not started)
```

**Auth is now mandatory on every route** (`Authorization: Bearer <token>`),
confirmed by the backend's endpoints doc — it used to be optional.
`ApiConfig.AuthToken` is still the single place that sets it; every call
will now fail (`401`) until it's set to a real token, so this needs wiring
up (e.g. from a login/config screen) before any real feature can use these
APIs, unlike before when things worked with no token at all.

`ChampionshipApi.cs` covers every endpoint under `/v1/championship/*`:
`GetChampionships`, `GetEvents`, `GetEventDetail`, `GetSessions`,
`GetSessionDetail`, `GetSessionDrivers`, `GetSessionTeams`, `GetStandings`
(`/sessions/{sessionId}/standings`), `GetDriverProfile` — the last one is
**global**, not session-scoped: `driverNumber` + optional
`championshipCode` filter, no `sessionId` at all (this was wrong before —
it used to require a `sessionId` it doesn't actually need). Every method
takes the caller-owned entity/`List<Entity>` as a parameter and updates it
in place — see [Data/Doc/README.md](../../Data/Doc/README.md) and
[Logic/Doc/UI-Refresh-Pattern.md](../../Logic/Doc/UI-Refresh-Pattern.md).

`ApiClient` now has both `Get<T>` and `Post<T>` (the latter added for
`race/control`, the one endpoint that isn't a plain GET — see below).
`onError` on both receives a `DataError` (`Data/Errors/DataError.cs`, kind
`Network` or `Deserialize`) rather than a raw string — see
[Data/Doc/README.md](../../Data/Doc/README.md#error-handling) for the full
error-handling picture across DTO/Mapper/Network.

## Communication technologies

Core will use **two distinct transports**, kept separate on purpose — they
solve different problems and forcing them into one generic system would hurt
both (see discussion that led to this split).

### Standard REST (implemented) — `Network/ApiClient.cs`

Used for anything requested occasionally: catalog, standings, driver
profiles, health checks. One HTTP request in, one JSON response out.

- **Transport**: `UnityWebRequest` (Unity's built-in HTTP client), driven by
  a coroutine so a call never blocks the main thread.
- **Serialization**: Newtonsoft Json.NET (`com.unity.nuget.newtonsoft-json`)
  instead of Unity's built-in `JsonUtility`, because gateway responses can be
  root-level JSON arrays and contain nullable fields — both unsupported by
  `JsonUtility`.
- **Cost model**: one request = one parse. Negligible for calls triggered by
  a screen open or a periodic refresh; would become a real problem
  (reflection-based parsing + HTTP overhead repeated every frame, GC
  pressure) if reused to poll fast-changing data.

### High-frequency live data — deliberately kept simple for now

A `WebSocket` module (`LiveSocket`, against a `WS /v1/race-data/live`
endpoint assumed from an earlier draft) was built once, then deliberately
reverted: too much complexity for where the project was at, with no real
screen consuming any of this yet. `RaceDataApi.cs` covers the
race/telemetry endpoints instead, using the exact same simple shape as
`ChampionshipApi.cs` (coroutine + `ApiClient.Get`/`Post`, REST, no
background thread).

The backend's endpoints doc has since revealed what the real high-frequency
mechanism actually is, and it's neither of those guesses:

- **`GET /sessions/{sessionId}/race/live/stream`** — a **Server-Sent
  Events** stream (`EventSource`, one-way server→client over plain HTTP),
  not a WebSocket. **Not implemented yet** — `UnityWebRequest` doesn't
  support SSE directly; would need a streaming `DownloadHandler` or
  `HttpClient` reading the response as a live stream. Build this only when
  a screen actually needs it, same reasoning as the reverted `LiveSocket`.
- **V1 semantics are a replay, not a live feed**: it replays a session's
  already-ingested historical data at `(real recorded interval) / speed`
  (query param, default `10`), capped at 5 real seconds between frames —
  there is no "currently live" race in this environment. A V2 may add a
  true live-tail mode; explicitly out of scope for now.
- **Three named SSE events**, via the `event:` field (not a JSON `"type"`
  key) — `telemetry` (combines speed+engine+location+`lapNumber` in one
  flat object per frame, a real answer to the earlier "does the generic
  telemetry dataset combine speed+engine?" question — for the *stream*, yes,
  but the historical REST endpoints below still return them separately),
  `position` (like `GetPositions`/`GetDriverPosition` but without
  `gapToLeader`), `raceControl` (same shape as `GetRaceControlEvents`), and
  a final `done` event (empty payload) when the replay ends. No DTOs exist
  for these yet — build them against this doc's shapes when the SSE client
  is actually built, not before.
- **Known limitation documented by the backend**: browser `EventSource`
  can't send custom headers, so it can't carry the now-mandatory
  `Authorization` header — a token-in-query-param or proxy would be needed
  for a browser client. Not relevant to a native mobile/Quest client, which
  can set its own headers.

`RaceDataApi.cs` covers, under `/v1/race-data/*`, using the endpoints'
**confirmed real paths** (not the generic `/datasets/{dataset}` pattern
assumed earlier from `gateway/ROUTES.md` — the backend's own doc uses
explicit named paths instead, matching the original draft):
`GetPositions`/`GetDriverPosition` (`/race/position` — object instead of
array when `driverNumber` is passed), `GetLaps`/`GetDriverLaps`
(`/race/laps` — same object/array behavior), `GetStints`, `GetPitStops`,
`GetWeather`, `GetTeamRadio` (always arrays), `GetRaceControlEvents`
(`POST /race/control`, confirmed long-poll: holds up to 30s, returns `[]`
on timeout rather than an error — `RaceDataApi` does one call per
invocation, the caller loops to keep listening), and driver-scoped
`GetDriverSpeedHistory`/`GetDriverEngineHistory`/`GetDriverLocationHistory`/
`GetDriverIntervals`. `speed`/`engine` are now wired — confirmed simple
per-sample shapes (`{speed, gear, timestamp}` / `{rpm, gear,
throttlePercent, brakePercent, drsActive, timestamp}`, no `battery` field
at all, confirmed no data source). Left out, no DTO yet: `result`,
`starting_grid`, `session_result`, `overtakes`, `car_data`.

## Allowed dependencies

Core can depend on [Data](../../Data/Doc/README.md) (to deserialize responses
into DTOs). It must never reference UI, Interaction or Logic.
