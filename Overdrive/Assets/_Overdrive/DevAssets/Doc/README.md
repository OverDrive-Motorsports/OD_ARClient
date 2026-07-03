# DevAssets

Prototyping/demo assets, unrelated to production logic: images and videos
used to populate the UI during development while waiting for real content
from the backend.

## Current state

Renamed from `DataMockup/`:

```
DevAssets/
├── Miniature/   Demo images (F1 driver/race thumbnails)
├── Video/       Demo video for the video player
├── VideoRenderTexture.renderTexture
└── F1-Logo.png  (recovered from an orphaned Assets/Assets/ folder, unclear origin — worth checking if still needed)
```

These assets are consumed by `Data/Mock/ODMockData.cs` and the UI display
components (e.g. `VideoPlayerController` in `Logic/Video/`).
