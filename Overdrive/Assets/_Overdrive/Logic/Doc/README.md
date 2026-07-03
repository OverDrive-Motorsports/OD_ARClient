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

## Allowed dependencies

Logic is the only layer allowed to depend on everything: Core, Data, UI,
Interaction. No other layer should depend on Logic.
