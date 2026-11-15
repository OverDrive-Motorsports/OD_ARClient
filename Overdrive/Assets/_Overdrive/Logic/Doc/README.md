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
├── Menu/     Navigation and main menu (AppLauncher, OverdriveMainMenu)
├── Ranking/  Live race ranking (RaceRankingManager)
├── Search/   Content search/filtering (SearchController)
└── Video/    Video playback (VideoPlayerController)
```

## Current state

Groups the former `Scripts/UI/` controllers that already did orchestration
(not just display): menu loading, ranking management, search, video
playback. They currently rely on the mocks in `Data/Mock/ODMockData.cs`
while waiting for a real backend.

See [UI-Refresh-Pattern.md](./UI-Refresh-Pattern.md) for how a screen should
hold and refresh its Entities (mutate in place, never replace),
[Example-EntityToODDataTable.md](./Example-EntityToODDataTable.md) for a
worked example wiring `StandingEntry` into the existing `ODDataTable`
prefab, and [RaceReplay-DownloadAndPlayback.md](./RaceReplay-DownloadAndPlayback.md)
for the different case of a full-race download consumed by seeking through
a local timeline instead of being refreshed.

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
  ID, not the display title or a fabricated mock session. The rest of the
  app can still show demo data without playing a demo video.

## Allowed dependencies

Logic is the only layer allowed to depend on everything: Core, Data, UI,
Interaction. No other layer should depend on Logic.
