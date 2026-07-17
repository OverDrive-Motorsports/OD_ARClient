/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## BuildOrchestrators - Entry points that chain the individual Tier 02/03/04
 ## builders in the correct dependency order, so the Overdrive menu only
 ## exposes 3 top-level commands beyond OD_AllUnit: Build Screens,
 ## Apply Visual Fixes, Build All. See each class's summary for exactly
 ## what it runs and in what order.
 ##
 */

using UnityEditor;
using UnityEngine;

/// <summary>
/// BUILD ORDER — Tier 05 (Orchestrator). Requires: 01_ODUIBuilder must already
/// have run (Build OD_UI Base) — this only chains Tier 02/03 screen builders,
/// it does not build atoms/molecules/organisms itself.
///
/// Runs every screen/sub-screen builder in dependency order: the Main Menu first
/// (Tier 02, so its hierarchy exists), then Home Nav and Championship Page
/// (Tier 02, need 01_ODUIBuilder prefabs), then the menu's sub-panels and
/// standalone widgets (Tier 03, need the Main Menu's hierarchy to attach to).
/// </summary>
public static class AllScreensBuilder
{
    [MenuItem("Overdrive/Build Screens", false, 20)]
    public static void BuildAllScreens()
    {
        // Tier 02 — screens (Main Menu first: Tier 03 sub-builders attach to it)
        MainMenuScreenBuilder.Build();
        HomeNavScreenBuilder.Build();
        ChampionshipPageScreenBuilder.Build();

        // Tier 03 — screen sub-builders / standalone widgets
        ProfilePanelScreenBuilder.Build();
        RankingWidgetScreenBuilder.Build();
        SearchPanelScreenBuilder.Build();
        VideoPlayerScreenBuilder.Build();

        Debug.Log("[AllScreensBuilder] All screens built successfully.");
        EditorUtility.DisplayDialog("Screen Builder", "Main Menu, Home Nav, Championship Page, Profile Panel, Ranking Widget, Search and Video Player built.", "OK");
    }
}

/// <summary>
/// BUILD ORDER — Tier 05 (Orchestrator). Requires: an "OverdriveMenuCanvas"
/// INSTANCE in the currently open scene, built by Tier 02 MainMenuScreenBuilder
/// (via AllScreensBuilder or on its own) — every Tier 04 fixer uses GameObject.Find
/// on that instance, not an asset path, and logs/skips if it's missing.
///
/// Applies every one-off visual touch-up pass (rounded corners, mockup
/// thumbnails, grid placeholders) in that order.
/// </summary>
public static class AllVisualFixesApplier
{
    [MenuItem("Overdrive/Apply Visual Fixes", false, 40)]
    public static void ApplyAll()
    {
        RoundedCornersFixer.Apply();
        MockupThumbnailsFixer.Apply();
        GridPlaceholdersFixer.Apply();
        Debug.Log("[AllVisualFixesApplier] Rounded corners, mockup thumbnails and grid placeholders applied.");
        EditorUtility.DisplayDialog("Visual Fixes", "Rounded corners, mockup thumbnails and grid placeholders applied.", "OK");
    }
}

/// <summary>
/// BUILD ORDER — Tier 05 (Orchestrator), no prerequisites: runs every tier itself,
/// in the only order that works — 01 (foundation prefabs) → 02/03 (screens) →
/// 04 (visual fixes that need those screens present in-scene). Use this for a
/// clean full rebuild; use OD_AllUnit or the individual Tier 02/03/04 items
/// instead when you only need to touch one piece.
/// </summary>
public static class FullBuildRunner
{
    [MenuItem("Overdrive/Build All", false, 60)]
    public static void BuildEverything()
    {
        ODUIBuilder.BuildBase();
        AllScreensBuilder.BuildAllScreens();
        AllVisualFixesApplier.ApplyAll();
        Debug.Log("[FullBuildRunner] Full build pipeline completed.");
    }
}
