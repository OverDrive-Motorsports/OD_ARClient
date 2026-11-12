# Core

Central point for backend communication and global services. Contains **no
business logic related to gameplay or UI**: Core knows how to talk to the
server, never what the application does with the data (that's
[Logic](../../Logic/Doc/README.md)'s job).

```
Core/
├── Network/    Gateway client (below)
├── Resources/  ODApiConfig.asset — git-ignored, it holds the gateway token
└── Services/   Global services (ODTime, ODPassthroughBlur)
```

## Network

Talks to the OverDrive gateway (`OD_Backend/gateway`, routes in
`gateway/ROUTES.md`). Everything is `async`/`await` and stays on Unity's main
thread, so callers can touch the scene right after an `await`.

- `ODApiConfig` — gateway URL (Editor / headset), `GATEWAY_AUTH_TOKEN`,
  championship code, request timeout, download stall timeout, live polling
  period. Loaded from `Core/Resources/ODApiConfig.asset`: create it with
  *Assets > Create > Overdrive > API Config*.
- `ODApiClient` — `UnityWebRequest` + Newtonsoft.
  - `Get<T>` parses the JSON on a worker thread (no frame drop on large
    bodies); a 404 or an empty body returns `default`.
  - `Download` streams a body straight into a file (`DownloadHandlerFile`),
    reports the bytes received, can be cancelled, and gives up after
    `downloadStallSeconds` without data rather than on a fixed timeout.
  - Every failure becomes an `ODApiException`: `Kind` (Unreachable /
    Unauthorized / HttpError / InvalidResponse), `StatusCode`, and `Code`,
    the backend's error code (`UPSTREAM_UNAVAILABLE`, `SESSION_NOT_FOUND`...).
- `ODBackendApi` — one typed method per route the app uses, returning DTOs
  exactly as the backend sends them (`Data/DTO`). Nothing else.

The headset reaches the backend through the machine's LAN IP
(`deviceBaseUrl`); plain HTTP is allowed in Player Settings for that.
There is no WebSocket yet: the live state is polled.

## Allowed dependencies

Core can depend on [Data](../../Data/Doc/README.md) (DTOs). It must never
reference UI, Interaction or Logic.
