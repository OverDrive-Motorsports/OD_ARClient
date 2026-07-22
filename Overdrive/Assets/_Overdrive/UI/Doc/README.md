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

`Prefabs/Screens/` groups full-screen prefabs (as opposed to reusable
Atoms/Molecules/Organisms components): `OverdriveMenuPanel`, `HomeNavScreen`
(persistent bottom nav + home mini-window), `ChampionshipPage` (an empty
skeleton — see below).

`Molecules/ODWeatherWidget.cs` and `Molecules/ODMapWidget.cs` are dedicated
"big" widgets for weather and circuit location — the map widget is a
placeholder for now (just shows "Carte" + the location), meant to be swapped
for a real interactive map later.

`Molecules/ODStandingsWidget.cs` is a generic standings card with a built-in
Pilotes/Écuries toggle. It has no dedicated prefab: it's built entirely at
runtime (`ODStandingsWidget.Create(...)`) by
[ChampionshipPageController](../../Logic/Doc/README.md), one instance per
category found in whatever `ChampionshipData.standings` list it's given —
it has no idea which championship it's rendering.

## Allowed dependencies

None towards Core/Data/Logic. UI may use Interaction only for input-related
rendering (e.g. hover visual feedback), never for decision-making.
