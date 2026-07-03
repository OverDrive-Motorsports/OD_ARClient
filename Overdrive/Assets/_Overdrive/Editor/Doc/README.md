# Editor

Editor-only scripts: build-time tools that generate or configure UI/scene
elements directly inside the Unity editor (rounded corners, GraphicRaycaster,
panel generation...). Not part of the application's runtime.

## Current state

Merge of the former `Scripts/Editor/` (panel generators: profile, ranking,
search, video player, main menu) and `OD_UI/Editor/ODUIBuilder.cs`.
`ODShowcaseBuilder.cs` (formerly in `OD_UI/Dev/`) was also brought here since
it's a demo-scene building tool, not a runtime component.

## Allowed dependencies

No strict rule: these are Editor scripts that can touch any layer to build
prefabs/scenes, but they are never compiled into the final build.
