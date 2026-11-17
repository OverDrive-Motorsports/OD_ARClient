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
└── Video/         Video playback (VideoPlayerController)
```

## Current state

Screens load their data through the services (`ODChampionshipDataService`,
`ODReplayService`), which call [Core](../../Core/Doc/README.md) and map the
DTOs into entities. There is no demo data: when the backend can't be reached,
`ODBackendErrorPopup` offers to retry.

See [RaceReplay-DownloadAndPlayback.md](./RaceReplay-DownloadAndPlayback.md)
for how a replay goes from the backend to the headset, then plays.

## Backend video (Dev contract)

- `Video/VideoPlayerController.cs` passes the selected backend session to
  the TV, manages only selected pilot decoders, pauses/resumes views, keeps
  auxiliary audio muted and applies transport controls to the promoted feed.
- `Video/ODRemoteVideoStream.cs` resolves either the main `broadcastUrl`
  (camelCase) or a driver's `broadcast_url` (snake_case), then prepares the
  HTTP(S) media URL in a native VideoPlayer. Empty/invalid URLs, unavailable
  backend and decoder failures show an unavailable state, never a local clip.
  Generation checks ignore late replies on hide/session change/disposal;
  closing a camera releases its decoder and allocated texture. The gateway
  Bearer token belongs only to JSON calls and is never attached to media URLs.
- `Menu/ODSportPage.VideoSessionId` uses the selected live session or replay
  ID, not the display title or a fabricated mock session. Downloaded race
  data continues to play independently through `ODReplayPlayer`, even when
  no broadcast is available; the removed demo mode is not restored.

## Allowed dependencies

Logic is the only layer allowed to depend on everything: Core, Data, UI,
Interaction. No other layer should depend on Logic.
