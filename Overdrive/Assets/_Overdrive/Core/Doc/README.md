# Core

Central point for backend communication: HTTP API, WebSocket, OAuth, and more
broadly any global service (runtime configuration, session...).

Contains **no business logic related to gameplay or UI**: Core knows how to
talk to the server and expose data/events, but never decides what the
application does with that data (that's [Logic](../../Logic/Doc/README.md)'s job).

## Current state

Empty for now: the project doesn't make any real network calls today, all
displayed data comes from static mocks (see [Data/Mock](../../Data/Doc/README.md)).
Suggested structure once the first networking need shows up:

```
Core/
├── Network/   HTTP/WebSocket clients, endpoint definitions
├── Auth/      OAuth, session/token management
└── Services/  Global services (SceneLoader, AppSettings...)
```

## Allowed dependencies

Core can depend on [Data](../../Data/Doc/README.md) (to deserialize responses
into DTOs). It must never reference UI, Interaction or Logic.
