# Core

Central point for backend communication: HTTP API, WebSocket, OAuth, and more
broadly any global service (runtime configuration, session...).

Contains **no business logic related to gameplay or UI**: Core knows how to
talk to the server and expose data/events, but never decides what the
application does with that data (that's [Logic](../../Logic/Doc/README.md)'s job).

## Current state

`Network/` talks to the OverDrive gateway (`OD_Backend/gateway`, routes in
`gateway/ROUTES.md`):

- `ODApiConfig` — gateway URL (Editor / headset), `GATEWAY_AUTH_TOKEN`,
  championship code, timeout, live polling period. Loaded from
  `Core/Resources/ODApiConfig.asset`, which is **git-ignored** (it holds the
  token): create it with *Assets > Create > Overdrive > API Config*.
- `ODApiClient` — async `UnityWebRequest` + Newtonsoft, Bearer header, every
  failure turned into an `ODApiException` (Unreachable / Unauthorized /
  HttpError / InvalidResponse).
- `ODBackendApi` — one typed method per route used by the app, returning DTOs.

The headset reaches the backend through the machine's LAN IP
(`deviceBaseUrl`); plain HTTP is allowed in Player Settings for that.
There is no WebSocket yet: the live state is polled.

Remaining folders to add when needed:

```
Core/
├── Network/   HTTP/WebSocket clients, endpoint definitions
├── Auth/      OAuth, session/token management
└── Services/  Global services (SceneLoader, AppSettings...)
```

## Allowed dependencies

Core can depend on [Data](../../Data/Doc/README.md) (to deserialize responses
into DTOs). It must never reference UI, Interaction or Logic.
