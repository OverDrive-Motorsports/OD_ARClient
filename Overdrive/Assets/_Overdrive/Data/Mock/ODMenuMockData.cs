/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMenuMockData - Static class providing every piece of fake data currently
 ## displayed by OverdriveMenuPanel (main menu screen).
 ##
 */

using System.Collections.Generic;

/// <summary>
/// Compile-time constants and static lists that populate the Overdrive main
/// menu (OverdriveMenuPanel / OverdriveMainMenu.cs) with plausible data.
/// Contains no MonoBehaviour, no serialized state, no AssetDatabase calls —
/// safe to reference from runtime code as well as Editor tooling.
/// </summary>
public static class ODMenuMockData
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Header
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>App title shown top-left of the menu panel.</summary>
    public static string MenuTitle = "Overdrive";

    /// <summary>Placeholder text shown inside the search bar before the user types.</summary>
    public static string SearchPlaceholder = "🎤 Search";

    // ─────────────────────────────────────────────────────────────────────────────
    // Sidebar navigation
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sidebar tab labels, in display order. Tab 0 ("Races") is the only one
    /// with real content today — the others show a "Coming soon" placeholder.
    /// </summary>
    public static List<string> NavTabs => new List<string>
    {
        "Races", "Rankings", "Profile", "Settings",
    };

    /// <summary>Placeholder message shown on tabs that have no content yet.</summary>
    public static string ComingSoonMessage(string tabLabel) => tabLabel + "\n<size=60%>Coming soon</size>";

    // ─────────────────────────────────────────────────────────────────────────────
    // Category selector (Races tab only)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Motorsport categories cycled through by clicking the category label.
    /// Only "F1" currently has race cards behind it — the rest are UI-only for now.
    /// </summary>
    public static List<string> RaceCategories => new List<string>
    {
        "F1", "MotoGP", "IndyCar", "F2",
    };

    // ─────────────────────────────────────────────────────────────────────────────
    // Race grid (Races tab content)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>One card in the Races grid: a race name paired with a thumbnail image.</summary>
    public struct RaceMenuItem
    {
        public string title;
        /// <summary>File name under Data/Mock source images — Assets/_Overdrive/DevAssets/Miniature/.</summary>
        public string thumbnailFileName;
    }

    /// <summary>
    /// Race cards shown in the grid. Thumbnails are generic stock motorsport
    /// photos (not race-specific — DevAssets/Miniature has no per-GP images),
    /// paired here explicitly instead of being zipped by index at build time.
    /// </summary>
    public static List<RaceMenuItem> RaceGridItems => new List<RaceMenuItem>
    {
        new RaceMenuItem { title = "Bahrain GP",       thumbnailFileName = "000_87cr3ph_1_69b5252f47670.jpg" },
        new RaceMenuItem { title = "Saudi Arabia GP",  thumbnailFileName = "1200-L-ferrari-la-scuderia-a-russi-battre-mercedes-son-propre-jeu-lors-du-grand-prix-du-japon.jpg" },
        new RaceMenuItem { title = "Australia GP",     thumbnailFileName = "1200-L-gp-du-portugal-de-formule-1-les-rsultats-des-essais-libres-3.jpg" },
        new RaceMenuItem { title = "Japan GP",         thumbnailFileName = "800-L-fernando-alonso-dans-les-points-bakou-lespagnol-est-heureux-de-sa-prestation.jpg" },
        new RaceMenuItem { title = "China GP",         thumbnailFileName = "CMPPT6WCCJA3NNAGCYGXKELSWE.jpg" },
        new RaceMenuItem { title = "Miami GP",         thumbnailFileName = "DPPI_00126008_2376-2.jpg" },
        new RaceMenuItem { title = "Monaco GP",        thumbnailFileName = "F1-valorisation-hausse-scaled.jpg" },
        new RaceMenuItem { title = "Spain GP",         thumbnailFileName = "UV5X4LVW4JEBPECNED66JK3T6E.jpg" },
        new RaceMenuItem { title = "Canada GP",        thumbnailFileName = "andrea-kimi-antonelli-mercedes-2.jpg" },
        new RaceMenuItem { title = "Austria GP",       thumbnailFileName = "charles-leclerc-ferrari-andrea.jpg" },
        new RaceMenuItem { title = "Silverstone GP",   thumbnailFileName = "images.jpg" },
    };
}
