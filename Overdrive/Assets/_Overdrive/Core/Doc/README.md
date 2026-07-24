# Core

Central point for backend communication: HTTP API, WebSocket, OAuth, and more
broadly any global service (runtime configuration, session...).

Contains **no business logic related to gameplay or UI**: Core knows how to
talk to the server and expose data/events, but never decides what the
application does with that data (that's [Logic](../../Logic/Doc/README.md)'s job).

## Current state

`Network/` holds every real network call so far: a gateway health check and
the championship endpoints. Nothing displayed in the app uses this yet
though — screens still read from static mocks (see
[Data/Mock](../../Data/Doc/README.md)); wiring a screen to `ChampionshipApi`
is the next step.

```
Core/
├── Network/
│   ├── ApiConfig.cs            Gateway base URL + optional Bearer token
│   ├── ApiClient.cs            Generic coroutine GET, deserializes with Newtonsoft
│   ├── GatewayHealthApi.cs     GET /health
│   ├── GatewayHealthCheck.cs   Manual test MonoBehaviour (not wired into app startup)
│   └── ChampionshipApi.cs      Championship endpoints (see below)
├── Auth/      OAuth, session/token management (not started)
└── Services/  Global services (SceneLoader, AppSettings...) (not started)
```

`ChampionshipApi.cs` covers every endpoint under `/v1/championship/*`
(`gateway/ROUTES.md`): `GetChampionships`, `GetEvents`, `GetEventDetail`,
`GetSessions`, `GetSessionDetail`, `GetSessionDrivers`, `GetSessionTeams`,
`GetStandings`, `GetDriverProfile` (`sessions/{sessionId}/drivers/{driverNumber}/profile`,
scoped by session rather than just by driver number). Every method takes
the caller-owned entity/`List<Entity>` as a parameter and updates it in
place — see [Data/Doc/README.md](../../Data/Doc/README.md) and
[Logic/Doc/UI-Refresh-Pattern.md](../../Logic/Doc/UI-Refresh-Pattern.md).

Two response shapes in the source doc aren't confirmed yet and were left
out: `GET /sessions/{sessionId}/broadcast` and
`GET /sessions/{sessionId}/datasets/{dataset}` have no documented JSON body,
so no DTO/method exists for them — add them once their shape is confirmed
rather than guessing. The standings path also differs between the two docs
(`/standings` in the draft vs `/standings/race` in `ROUTES.md`); the gateway
path is used, on the assumption the JSON shape is the same — flag this if a
real call proves otherwise.

The gateway (`gateway/` service, see its `ROUTES.md`) currently accepts
requests with no `Authorization` header, but will require a Bearer token
later — `ApiConfig.AuthToken` is the single place that changes when that
happens; `ApiClient` already reads it on every request.

`ApiClient.Get<T>`'s `onError` callback receives a `DataError`
(`Data/Errors/DataError.cs`, kind `Network` or `Deserialize`) rather than a
raw string — see [Data/Doc/README.md](../../Data/Doc/README.md#error-handling)
for the full error-handling picture across DTO/Mapper/Network.

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

### High-frequency live data (planned, not implemented) — telemetry

Needed for the live race/telemetry endpoints (position, speed, engine,
location... at up to 10-60Hz). REST polling is the wrong tool here: each
value would cost a full HTTP round-trip plus a reflection-based JSON parse,
which on a VR headset means GC-driven frame hitches (motion sickness risk).

- **Transport**: WebSocket — the gateway already exposes
  `WS /v1/race-data/live?sessionId={sessionId}` for exactly this: one
  persistent connection, server pushes updates, no repeated request
  overhead.
- **Status**: not built yet. Will live under `Network/` alongside `ApiClient`
  as a separate module (e.g. `LiveSocket`), not as an extension of it —
  it needs its own connection lifecycle (open/close/reconnect) and a
  low-allocation message path, neither of which the REST path needs.

## Allowed dependencies

Core can depend on [Data](../../Data/Doc/README.md) (to deserialize responses
into DTOs). It must never reference UI, Interaction or Logic.
