# Logic

Contains the application's business logic. Receives events coming from
[Interaction](../../Interaction/Doc/README.md) or [UI](../../UI/Doc/README.md),
decides which actions to take and orchestrates the overall behavior.
Communicates with [Core](../../Core/Doc/README.md) for backend calls and
updates the UI accordingly. The middle layer between data ([Data](../../Data/Doc/README.md)),
interactions and display.

## Structure

```
Logic/
├── Championship/  Championship panel data and states (ODChampionshipDataService)
├── Menu/          Navigation and main menu (AppLauncher, OverdriveMainMenu)
├── Ranking/       Live race ranking (RaceRankingManager)
├── Replay/        Replay catalogue, podium, download to the headset (ODReplayService) and playback clock (ODReplayPlayer)
├── Telemetry/     ODWidgetSpace (widgets added from the catalogue: one movable window each), ODTelemetryFeed (backend telemetry of the active session, pushed to them) and ODMockTelemetry (made-up still sample for the catalogue previews only)
└── Video/         Video playback (VideoPlayerController)
```

## Current state

Screens load their data through the services (`ODChampionshipDataService`,
`ODReplayService`), which call [Core](../../Core/Doc/README.md) and map the
DTOs into entities. There is no demo data: when the backend can't be reached,
`ODBackendErrorPopup` offers to retry.

See [RaceReplay-DownloadAndPlayback.md](./RaceReplay-DownloadAndPlayback.md)
for how a replay goes from the backend to the headset, then plays.

## Telemetry widgets

Widgets placed in the space show the backend telemetry of the active session
(`ODTelemetryFeed`, one per `ODWidgetSpace`). Every widget follows
Verstappen (`DriverCode` "VER"); the ones comparing two cars put Hamilton
beside him (`RivalCode` "HAM"). `ODSportPage.SyncTelemetry` picks the source:

- replay state: the downloaded replay (`ODReplayPlayer.Replay`), read at the
  replay clock (`RaceTime`). Each frame is computed from the time alone
  (`ODTelemetryMapper.Fill`), so the widgets follow the playback, and follow
  it back when it is rewound or moved;
- live state: the gateway routes of the live session — `/race/laps` (every
  car), then `/telemetry/speed` and `/telemetry/engine` of the running,
  previous and best lap of each followed car — polled every
  `livePollSeconds`, played `livePollSeconds + 2 s` behind the newest sample;
- elsewhere: nothing (the space is hidden).

States: loading, no data (the session has none for the car, or not at this
time) and connection lost (2 failed polls in a row) hide every value under a
message; nothing stale stays on screen. The catalogue previews keep made-up
data (`ODMockTelemetry.Sample`).

## Allowed dependencies

Logic is the only layer allowed to depend on everything: Core, Data, UI,
Interaction. No other layer should depend on Logic.
