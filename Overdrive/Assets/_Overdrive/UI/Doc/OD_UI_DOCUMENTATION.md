# OD_UI — Unity UI Component Library

## Overview

OD_UI is the canonical UI component library for the OverDrive 2026 project. It provides a complete set of Unity UGUI components purpose-built for world-space VR canvases on Meta Quest hardware.

**Design philosophy:**
- **Atomic Design** — every visual element is built bottom-up: Theme → Atoms → Molecules → Organisms.
- **Glassmorphism** — mid-dark frosted panels with translucent backgrounds, blur effects, and gold accents.
- **Multi-championship** — all data components (ODDataTable, ODDriverCard) are column/schema-agnostic so the same prefabs serve F1, WRC, Formula E, and any future championship without code changes.
- **Zero external dependencies** — no DOTween, LeanTween, or Shader Graph. Coroutines only for animation.
- **Single source of truth** — all colors, sizes, and shape values live in one `UITheme.asset`; every component reads `UITheme.Instance` at runtime.

---

## Architecture

### Level 0 — Theme

| File | Purpose |
|---|---|
| `Theme/UITheme.cs` | `ScriptableObject` singleton. Holds all brand colors, type sizes, shape values. |
| `Resources/UITheme.asset` | The live asset. Created by `ODUIBuilder`. Loaded via `Resources.Load<UITheme>("UITheme")`. |

All runtime color/size access goes through `UITheme.Instance`. Never hardcode colors in component code.

### Level 1 — Atoms

Smallest visual units. Each wraps one Unity primitive (Image, TextMeshProUGUI, RawImage) and applies UITheme values.

| Component | Description |
|---|---|
| `ODBackground` | Themed Image with four style presets (Card / Modal / Subtle / Alt). Adds blur automatically. |
| `ODLabel` | TextMeshProUGUI with H1 / H2 / Body / Caption presets. |
| `ODIcon` | Image + Sprite with optional theme-tinted color. Defaults to accentGold. |
| `ODDivider` | 2px full-width horizontal separator in borderColor. |
| `ODBlurBackground` | CPU-based blur snapshot effect. Attached automatically by ODBackground. |
| `ODGoldBorder` | Programmatic rounded gold gradient border texture. Place as child of the panel to outline. |
| `ODLiveBadge` | "LIVE / OFFLINE" pill with pulsing dot animation. |
| `ODBackgroundMedia` | Three-layer media background: raw image → blur overlay → gradient vignette. |
| `ODTelemetryCell` | Caption + large value pair. Animates value changes with a scale-punch. |

### Level 2 — Molecules

Composed from 2–4 atoms with interactive behavior.

| Component | Description |
|---|---|
| `ODButton` | Primary / Ghost / Danger styles. VR pointer events, click scale-punch. |
| `ODInputField` | TMP_InputField wrapper. Border animates to gold on focus. |
| `ODBadge` | Compact status pill: Default / Gold / Danger / Success variants. |

### Level 3 — Organisms

Complex components composed of multiple molecules and atoms.

| Component | Description |
|---|---|
| `ODCard` | Floating panel with title, divider, close button, and free content area. |
| `ODModal` | Full-screen blocking dialog wrapping an ODCard. Animates scale + alpha. |
| `ODNavBar` | Bottom navigation bar managing tab selection across ODNavItem children. |
| `ODNavItem` | Individual nav tab (icon + label, gold when selected). |
| `ODTableColumn` | *(Serializable data class, not MonoBehaviour)* Column schema for ODDataTable. |
| `ODTableRow` | Single data row. Builds TMP cells dynamically from column definitions. |
| `ODDataTable` | Generic table organism. Accepts any column/row schema. Highlight, tint, and live patch cells. |
| `ODDriverCard` | Driver card with team branding and 2-column telemetry grid. |
| `ODMediaControls` | 400×72 pill media bar: Rewind / Play-Pause / Forward / Live badge / Slider. |

---

## Visual Style

### Mid-Dark Glassmorphism

Panels use semi-transparent dark colors layered over the scene with a CPU box-blur effect.

| Token | Hex / RGBA | Usage |
|---|---|---|
| `panelBackground` | rgba(28,28,32, 0.78) | Primary card background |
| `panelBackgroundAlt` | rgba(36,36,42, 0.85) | Elevated/secondary panels |
| `surfaceColor` | rgba(44,44,52, 0.90) | Modal / deep surface |
| `subtleColor` | rgba(20,20,24, 0.50) | Low-emphasis overlays |
| `textPrimary` | #F2F2F7 | Body and heading text |
| `textSecondary` | #8E8E93 | Captions, placeholders, inactive tabs |
| `textTertiary` | #48484A | De-emphasized labels |
| `accentGold` | #C9A84C | Position highlights, active state, driver numbers |
| `blueColor` | #0A84FF | Interactive blue (future use) |
| `dangerColor` | #E8002D | Danger actions, LIVE dot |
| `successColor` | #32D74B | Positive status |
| `borderColor` | rgba(255,255,255, 0.10) | Hairline borders and Ghost button outline |
| `goldBorderColorA` | #C9A84C @ 60% | ODGoldBorder gradient bright end |
| `goldBorderColorB` | #C9A84C @ 8% | ODGoldBorder gradient dim end |

### Typography

| Token | Size (px) | Weight | Usage |
|---|---|---|---|
| `h1Size` | 36 | Bold | Display headings, driver numbers |
| `h2Size` | 28 | Bold | Section headings, telemetry values |
| `bodySize` | 22 | Normal | Table cells, card body text |
| `captionSize` | 18 | Normal / Bold | Column headers, badge labels, captions |

### Gold Border

`ODGoldBorder` generates a 128×128 RGBA32 texture at `OnEnable()` using a signed-distance-field rounded-rect algorithm. The border ring is filled with a diagonal gradient from `goldBorderColorA` (top-left) to `goldBorderColorB` (bottom-right). The component extends 2px beyond its parent by default (`oversize` field).

---

## Component Reference

---

### ODBackground
**Purpose:** Applies one of four themed background colors to the attached Image, and auto-adds blur.
**Prefab path:** `Assets/OD_UI/Prefabs/Atoms/ODBackground.prefab`

**Inspector fields:**
- `backgroundStyle` — Card | Modal | Subtle | Alt

**Key public methods:**
- `SetStyle(Style style)` — Switch style and repaint immediately.
- `SetAlpha(float a)` — Override alpha without changing the theme color.

**Usage example:**
```csharp
var bg = GetComponent<ODBackground>();
bg.SetStyle(ODBackground.Style.Alt);
```

---

### ODLabel
**Purpose:** TextMeshProUGUI preset with H1/H2/Body/Caption styles from UITheme.
**Prefab path:** `Assets/OD_UI/Prefabs/Atoms/ODLabel.prefab`

**Key public methods:**
- `SetText(string text)` — Update displayed string.
- `SetStyle(TextStyle style)` — Switch style and reapply theme values.

**Usage example:**
```csharp
var label = GetComponent<ODLabel>();
label.SetStyle(ODLabel.TextStyle.H1);
label.SetText("OverDrive");
```

---

### ODButton
**Purpose:** Themed interactive button with Primary / Ghost / Danger variants and VR pointer support.
**Prefab path:** `Assets/OD_UI/Prefabs/Molecules/ODButton_Primary.prefab` (also Ghost, Danger)

**Inspector fields:**
- `buttonStyle` — Primary | Ghost | Danger
- `OnClick` — UnityEvent fired on pointer click

**Key public methods:**
- `SetLabel(string text)` — Change button text.
- `SetStyle(ButtonStyle style)` — Repaint with new variant.

**Usage example:**
```csharp
var btn = GetComponent<ODButton>();
btn.SetLabel("JOIN RACE");
btn.OnClick.AddListener(() => Debug.Log("Clicked!"));
```

---

### ODDataTable
**Purpose:** Generic modular table organism. Any column schema, live cell patching, row tinting.
**Prefab path:** `Assets/OD_UI/Prefabs/Organisms/ODDataTable.prefab`

**Key public methods:**
- `SetColumns(List<ODTableColumn>)` — Define schema. Must be called before SetData.
- `SetData(List<List<string>>, List<string> rowIds)` — Populate all rows.
- `AppendRow(List<string>, string rowId)` — Add one row and return its ID.
- `ClearData()` — Remove all rows, preserve header.
- `SetHighlightedRow(string rowId)` — Gold accent on one row, clear others.
- `SetRowTint(string rowId, Color)` — Team-color tint at 10% alpha.
- `UpdateCell(string rowId, string columnId, string value)` — Live patch without rebuild.

**Usage example:**
```csharp
var table = GetComponent<ODDataTable>();
table.SetColumns(ODMockData.F1TimingColumns);
table.SetData(ODMockData.F1TimingRows);
table.SetHighlightedRow("VER");
```

---

### ODDriverCard
**Purpose:** Compact driver information card with team branding and a 2-column telemetry grid.
**Prefab path:** `Assets/OD_UI/Prefabs/Organisms/ODDriverCard.prefab`

**Key public methods:**
- `SetDriver(string number, string name)` — Update number and name text.
- `SetTeam(Sprite logo, Color color)` — Apply team logo and color bar tint.
- `SetTelemetry(List<TelemetryCellData>)` — Rebuild the telemetry grid.
- `UpdateTelemetryValue(int index, string value)` — Live patch one cell with animation.

**Usage example:**
```csharp
var card = GetComponent<ODDriverCard>();
card.SetDriver("16", "Charles Leclerc");
card.SetTeam(null, new Color(0.8f, 0.05f, 0.05f));
card.SetTelemetry(ODMockData.LeclercTelemetry);
```

---

### ODMediaControls
**Purpose:** Pill-shaped transport bar with play/pause, rewind, forward, live badge, and seek slider.
**Prefab path:** `Assets/OD_UI/Prefabs/Organisms/ODMediaControls.prefab`

**Key public methods:**
- `SetPlaying(bool)` — Toggle play/pause icon.
- `SetLive(bool)` — Switch badge between LIVE and OFFLINE.
- `SetProgress(float 0–1)` — Move the seek slider.

**Usage example:**
```csharp
var mc = GetComponent<ODMediaControls>();
mc.SetLive(true);
mc.OnSeek.AddListener(t => Debug.Log($"Seek to {t:P0}"));
```

---

### ODGoldBorder
**Purpose:** Programmatic rounded gold gradient border. Place as a child of the panel to outline.
**Prefab path:** `Assets/OD_UI/Prefabs/Atoms/ODGoldBorder.prefab`

**Inspector fields:**
- `oversize` — How many units the border extends beyond the parent (default 2).
- `texSize` — Texture resolution (default 128).
- `animatePulse` — Enable alpha sine oscillation.

**Usage:** Drop as a child of any panel. No code required.

---

## Mock Data & Development

### ODMockData (`Assets/OD_UI/Dev/ODMockData.cs`)

Static class with fake data for every component type. No MonoBehaviour, no runtime dependencies.

| Property | Type | Description |
|---|---|---|
| `F1TimingColumns` | `List<ODTableColumn>` | 5-column F1 timing tower schema |
| `F1TimingRows` | `List<List<string>>` | 20-driver timing tower rows |
| `WRCStandingsColumns` / `WRCStandingsRows` | — | 10-driver WRC standings |
| `FormulaEColumns` / `FormulaERows` | — | 10-driver Formula E energy table |
| `LeclercTelemetry` | `List<TelemetryCellData>` | 6 telemetry cells for Leclerc card |
| `VerstappenTelemetry` | — | 6 telemetry cells for Verstappen card |
| `MockIsLive`, `MockProgress` | `bool`, `float` | Media controls initial state |
| `MockSessionTitle`, `MockLapInfo`, `MockChampionship` | `string` | Session header strings |

### ODShowcaseBuilder (`Assets/OD_UI/Dev/ODShowcaseBuilder.cs`)

Editor script. Run **Overdrive > Build OD_UI Showcase** to create an `OD_UI_Showcase` GameObject in the active scene containing all components in three side-by-side columns, fully populated with `ODMockData`.

**Requirements:** Run all builder menus first so prefabs exist:
1. **Overdrive > Build OD_UI Prefabs** — atoms + molecules
2. **Overdrive > Build OD_UI Organisms** — ODCard, ODModal, ODNavBar
3. **Overdrive > Build OD_UI Level3** — table, driver card, media controls, etc.
4. **Overdrive > Build OD_UI Showcase** — generates the scene preview

---

## Adding a New Component

1. **Create the script** in the correct folder:
   - `Assets/OD_UI/Atoms/` — wraps a single primitive
   - `Assets/OD_UI/Molecules/` — 2–4 atoms with interaction
   - `Assets/OD_UI/Organisms/` — full UI section or card

2. **Add the OverDrive file header** at the very top:
   ```
   /**
    ##
    ## OverDrive 2026
    ## All Technical rights reserved
    ##
    ## MyComponent - One-line description.
    ##
    */
   ```

3. **Use UITheme.Instance for all visual values.** Never hardcode a color or size.
   ```csharp
   UITheme theme = UITheme.Instance;
   if (theme != null) _image.color = theme.accentGold;
   ```

4. **Add XML doc comments** to every public class, method, property, and enum:
   ```csharp
   /// <summary>Applies the current variant to child visual components.</summary>
   private void Apply() { … }
   ```

5. **Register in ODUIBuilder.** Either add a call in an existing `Build*Prefabs()` method, or add a new `[MenuItem]` entry for Level3+ components.

6. **Add mock data to ODMockData** if your component needs example content.

7. **Add to ODShowcaseBuilder** so it appears in the visual regression preview.

---

## Contribution Guidelines

### Code style
- Follow standard Unity C# conventions (PascalCase for classes/methods, camelCase for locals).
- Use `[Header("…")]`, `[SerializeField]`, `[RequireComponent]` where appropriate.
- Keep methods short — single responsibility.

### Colors & sizes
- **Never hardcode** colors or font sizes. Always read from `UITheme.Instance`.
- Baked constants in `ODUIBuilder.cs` are the **only** exception — they must exactly mirror UITheme defaults so prefabs look correct before a theme asset is loaded.

### Animations
- Use **coroutines only** (`IEnumerator` + `yield return null`). No DOTween, no LeanTween.
- Always call `StopAllCoroutines()` before starting a state-transition coroutine to avoid ghost animations.

### Dependencies
- **No external packages.** The library must compile with only the packages already in `Packages/manifest.json`.

### API documentation
- Every public class, method, field, and enum must have a `/// <summary>` XML doc comment.
- Inline `//` comments are allowed for non-obvious logic (algorithm details, Unity quirks, workarounds).

### Testing
- After any change, run all three builder menu items to verify prefabs save without errors.
- Run **Overdrive > Build OD_UI Showcase** and inspect the result in the Scene view.
