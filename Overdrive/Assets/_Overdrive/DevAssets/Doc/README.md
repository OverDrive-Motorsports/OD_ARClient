# DevAssets

Prototyping/demo assets, unrelated to production logic: images
used to populate the UI during development while waiting for real content
from the backend.

## Current state

Renamed from `DataMockup/`:

```
DevAssets/
├── Miniature/   Demo images (F1 driver/race thumbnails)
├── VideoRenderTexture.renderTexture  TV output surface (not a video file)
└── F1-Logo.png  (recovered from an orphaned Assets/Assets/ folder, unclear origin — worth checking if still needed)
```

The former bundled demo MP4 has been removed. Video playback now resolves
the selected session/driver broadcast through the backend Dev endpoints;
there is no local video fallback. The RenderTexture is still required to
display the main stream and must not be deleted with the clip.
