# Editor

Editor-only scripts, never compiled into the build.

- **One-time setups** (`*Setup.cs`): authoring helpers that produced the saved
  prefabs and scene references (circuit preview, canvas interaction). They are
  not part of a build pipeline: running one again overwrites the hand edits
  made since. The category / replay / course-media setups were removed with
  the demo mode.
- **Verifications** (`*Verification.cs`): isolated editor smoke tests and
  screenshots (canvas interaction); they never change the open scene.
- `ODFontAssetsSetup.cs`: creates the DM Sans TextMesh Pro assets
  (`UI/Resources/Fonts`) from `Art/Fonts/DMSans`.

## Allowed dependencies

No strict rule: these scripts can touch any layer to build prefabs/scenes.
