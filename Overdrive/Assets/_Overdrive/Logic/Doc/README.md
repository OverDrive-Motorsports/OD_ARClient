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
(`ODTelemetryFeed`, one per `ODWidgetSpace`). Each widget follows its own car,
its `WidgetTarget` (Data):

- `Position` — whoever runs at that place, changing on its own with the
  running order. The default: P1 (the leader), and P1 and P2 for a widget
  comparing two cars;
- `Driver` — one car by number, picked in the running order.

The widget's driver pill opens the driver picker
(`UI/Organisms/ODDriverPickerView`) on the widget's right, tops aligned and
in the widget's plane; it never opens on its own. A widget comparing two
cars has two slots, "Followed" and "Compared": picking the followed car
moves on to the compared one, and a car already in the other slot swaps
places with it. One picker is open at a time; a choice closes it. The
widget's × removes it. Saved layouts and presets are not done yet.

The feed resolves each target (`Resolve`), keeps the running order at the
current time (`Order`: replay positions, or `/race/position` live) and fills
each widget's own frame (`Fill`). `ODSportPage.SyncTelemetry` picks the source:

- replay state: the downloaded replay (`ODReplayPlayer.Replay`), read at the
  replay clock (`RaceTime`). Each frame is computed from the time alone
  (`ODTelemetryMapper.Fill`), so the widgets follow the playback, and follow
  it back when it is rewound or moved;
- live state: the gateway routes of the live session — `/race/laps` and
  `/race/position` (every car), then `/telemetry/speed` and
  `/telemetry/engine` of the running, previous and best lap of each car a
  widget follows — polled every `livePollSeconds` (at once when a widget
  starts following a car not fetched yet), played `livePollSeconds + 2 s`
  behind the newest sample;
- elsewhere: nothing (the space is hidden).

States, per widget: loading, no data (the session has none for its car, or
not at this time) and connection lost (2 failed polls in a row) hide its
values under a message; nothing stale stays on screen. The catalogue previews keep made-up
data (`ODMockTelemetry.Sample`).

## Allowed dependencies

Logic is the only layer allowed to depend on everything: Core, Data, UI,
Interaction. No other layer should depend on Logic.
