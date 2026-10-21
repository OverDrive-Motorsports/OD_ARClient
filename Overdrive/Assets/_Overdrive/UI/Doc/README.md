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

## Design system (OverDrive VR v2)

The championship catalogue, championship pages (hors direct, live week-end,
live course, replay), replay gallery, profile / settings, error popup and
navigation bar follow the Canva design "OverDrive VR – v2" (pages
01 · Fondations and 01 · Composants):

- `Theme/ODDesign.cs` holds the tokens and runtime builders. Mockups draw a
  window at 1920 x 1200 px; `ODDesign.Px()` turns design px into window units
  (the window is 1200 x 750 units, `ODDesign.WindowSize`).
- Typeface: DM Sans (`Art/Fonts/DMSans`, OFL), TextMesh Pro assets generated
  by `Editor/ODFontAssetsSetup.cs` into `Resources/Fonts`.
- `Theme/ODChampionshipTheme.cs` applies the materials: glass window tinted by
  the championship, white 7 % plateaux, ornament bar, status pills.
- Frosted glass (`Resources/Shaders/ODGlass.shader`): `Core/Services/ODPassthroughBlur`
  blurs the Quest 3 passthrough camera and stitches each frame into a
  world-anchored panorama (`ODGlassPanorama.shader`); the glass looks it up in
  the direction behind each pixel, so it stays frosted when the panel leaves
  the camera's field of view. Pages that show glass call `Acquire` / `Release`.
- Controls: `Molecules/ODSegmentedControl` (recessed track, white 24 % pill;
  `solidPill` for the Profil / Réglages switch), `Atoms/ODToggleSwitch`
  (identity blue), `ODHoverState` (gaze ring, press scale) on design buttons.
- Page layouts sit next to their display code:
  `Organisms/ODChampionshipPanel.Layout.cs`, `Organisms/ODChampionshipTile.cs`,
  `Organisms/ODProfileView.cs`, `Logic/Menu/ODCategoriesPage.cs`,
  `Logic/Menu/ODReplayCatalog.cs`.
- Replays: the championship header's "▶ Replays" capsule (every state, backend
  or demo data) opens the gallery: a search field, then two sections,
  "Replays" (everything available) and "Mes replays" (saved with the card's
  star, kept on the headset in PlayerPrefs — no user endpoint yet). Simple
  glass cards, no motion (`Organisms/ODReplayTile.cs`). Picking one shows the
  broadcast page (championship panel in its replay state: video card,
  podium, circuit line, "Résultat final"), whose back arrow returns to the
  gallery. The demo button only cycles hors week-end / week-end / live.

## Allowed dependencies

None towards Core/Data/Logic. UI may use Interaction only for input-related
rendering (e.g. hover visual feedback), never for decision-making.
