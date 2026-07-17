using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BUILD ORDER — Tier 04 (Visual fix). Requires: an "OverdriveMenuCanvas" INSTANCE
/// already present in the currently open scene (this uses GameObject.Find, not an
/// AssetDatabase prefab lookup) — run 02_MainMenuScreenBuilder and drop its saved
/// prefab into the scene first, or this logs warnings and skips each missing path.
///
/// Swaps plain Image components for RoundedImage on the Overdrive menu
/// to give it a smooth, Apple/visionOS-style look.
/// Idempotent: safe to run multiple times.
/// </summary>
public static class RoundedCornersFixer
{
    private const string ROOT = "OverdriveMenuCanvas/OverdriveMenuPanel";

    public static void Apply()
    {
        // Panel + search bar + sidebar (decorative, not clickable)
        Round(ROOT,                       radius: 36f, raycast: false);
        Round(ROOT + "/SearchBar",        radius: 20f, raycast: false);
        Round(ROOT + "/Body/Sidebar",     radius: 26f, raycast: false);
        Round(ROOT + "/Body/ContentArea", radius: 24f, raycast: false);

        // Nav buttons → rounded pill highlight (must stay clickable)
        string[] navs = { "Nav_Races", "Nav_Rankings", "Nav_Profile", "Nav_Settings" };
        foreach (string nav in navs)
            Round(ROOT + "/Body/Sidebar/" + nav, radius: 14f, raycast: true);

        Debug.Log("[RoundedCorners] Applied — panel, search bar, sidebar & nav buttons.");
    }

    /// <summary>
    /// Replaces the Image at <paramref name="path"/> with a RoundedImage,
    /// preserving colour, sprite, Button.targetGraphic and raycast setting.
    /// </summary>
    private static void Round(string path, float radius, bool raycast)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning("[RoundedCorners] Not found: " + path); return; }

        // Already rounded → just update the radius
        var existing = go.GetComponent<RoundedImage>();
        if (existing != null)
        {
            existing.cornerRadius   = radius;
            existing.raycastTarget  = raycast;
            EditorUtility.SetDirty(go);
            return;
        }

        Image img = go.GetComponent<Image>();
        if (img == null) { Debug.LogWarning("[RoundedCorners] No Image on: " + path); return; }

        // Capture state before destroying
        Color  col    = img.color;
        Sprite sprite = img.sprite;
        var    btn    = go.GetComponent<Button>();

        Object.DestroyImmediate(img);

        RoundedImage r  = go.AddComponent<RoundedImage>();
        r.color         = col;
        r.sprite        = sprite;
        r.cornerRadius  = radius;
        r.cornerSegments = 12;
        r.raycastTarget = raycast;

        // Re-link the button's target graphic (lost when the old Image was destroyed)
        if (btn != null) btn.targetGraphic = r;

        EditorUtility.SetDirty(go);
    }
}
