# Editor

Editor-only scripts: build-time tools that generate or configure UI/scene
elements directly inside the Unity editor (rounded corners, GraphicRaycaster,
panel generation...). Not part of the application's runtime.

## Current state

Merge of the former `Scripts/Editor/` (panel generators: profile, ranking,
search, video player, main menu) and `OD_UI/Editor/ODUIBuilder.cs`. Also
includes:
- `HomeNavBuilder.cs` — assembles the persistent bottom nav bar and floating
  home mini-window screen out of the `ODNavBar`/`ODCard` organisms.
- `ChampionshipPageBuilder.cs` — assembles the championship page's 3 status
  panels (Live/RaceWeekend/Idle) out of `ODCard`/`ODDataTable`/`ODButton` plus
  the `ODWeatherWidget`/`ODMapWidget` molecules.

## Allowed dependencies

No strict rule: these are Editor scripts that can touch any layer to build
prefabs/scenes, but they are never compiled into the final build.
