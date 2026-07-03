# UI

Responsible for visual display, organized as Atomic Design. Prefabs and
scripts in this folder are **passive**: they display data and emit events
(click, value change...), but never make decisions and never access the
backend directly. [Logic](../../Logic/Doc/README.md) feeds the UI and reacts
to its events.

Detailed design system documentation: [OD_UI_DOCUMENTATION.md](OD_UI_DOCUMENTATION.md).

## Structure

```
UI/
├── Atoms/       Simplest UI components (label, icon, live badge, rounded image...)
├── Molecules/   Assemblies of atoms (button, input field, grid item...)
├── Organisms/   Complex components (driver card, data table, navbar, modal...)
├── Theme/       Theme ScriptableObject (colors, styles)
├── Resources/   Theme assets loaded via Resources.Load (UITheme.asset)
└── Prefabs/     Prefabs mirroring Atoms/Molecules/Organisms + Screens/ (full screens)
```

## Current state

Content mostly comes from the former `OD_UI/` (Atomic Design already in
place) merged with the visual scripts that used to live in `Scripts/UI/`
(`RoundedImage`, `ContentGridItem`, `ContentGridView`, `RaceStandingCard`),
which are genuinely display components and had no better home.

`Prefabs/Screens/OverdriveMenuPanel.prefab` groups full-screen prefabs (as
opposed to reusable Atoms/Molecules/Organisms components).

## Allowed dependencies

None towards Core/Data/Logic. UI may use Interaction only for input-related
rendering (e.g. hover visual feedback), never for decision-making.
