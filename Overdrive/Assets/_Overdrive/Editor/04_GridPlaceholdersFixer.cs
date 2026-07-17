using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BUILD ORDER — Tier 04 (Visual fix). Requires: an "OverdriveMenuCanvas" INSTANCE
/// already present in the currently open scene (GameObject.Find, not a prefab
/// lookup) with its grid built by 02_MainMenuScreenBuilder — aborts with an error
/// if GridContent isn't found.
///
/// Tints each grid placeholder with a distinct accent color + adds a play icon
/// and race name bar, purely so mock grid items are visually distinguishable
/// before real thumbnails exist (see 04_MockupThumbnailsFixer for the real ones).
/// </summary>
public static class GridPlaceholdersFixer
{
    // Thumbnail accent colors to differentiate placeholders
    static Color[] accentColors = {
        new Color(0.55f, 0.15f, 0.10f, 1f), // rouge F1
        new Color(0.10f, 0.25f, 0.55f, 1f), // bleu
        new Color(0.15f, 0.45f, 0.20f, 1f), // vert
        new Color(0.50f, 0.35f, 0.05f, 1f), // orange
        new Color(0.35f, 0.10f, 0.55f, 1f), // violet
        new Color(0.10f, 0.40f, 0.50f, 1f), // cyan
    };

    static string[] raceNames = {
        "Bahrain GP", "Saudi Arabia GP", "Australia GP",
        "Japan GP",   "China GP",        "Miami GP"
    };

    public static void Apply()
    {
        GameObject gridContent = GameObject.Find(
            "OverdriveMenuCanvas/OverdriveMenuPanel/Body/ContentArea/GridScrollView/Viewport/GridContent");

        if (gridContent == null)
        {
            Debug.LogError("GridContent not found!");
            return;
        }

        int idx = 0;
        foreach (Transform child in gridContent.transform)
        {
            EnhanceItem(child.gameObject, idx);
            idx++;
        }

        EditorUtility.SetDirty(gridContent);
        Debug.Log($"Enhanced {idx} grid placeholders.");
    }

    static void EnhanceItem(GameObject item, int idx)
    {
        Color accent = accentColors[idx % accentColors.Length];
        string race  = raceNames[idx % raceNames.Length];

        // ── Thumbnail (fill entire item) ──────────────────────────────────
        Transform thumbT = item.transform.Find("Thumbnail");
        if (thumbT != null)
        {
            Image thumbImg = thumbT.GetComponent<Image>();
            if (thumbImg != null)
            {
                thumbImg.color = accent;
                // stretch to fill
                RectTransform rt = thumbT.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        // ── Play icon ▶ centered ──────────────────────────────────────────
        GameObject playGO = GetOrCreate("PlayIcon", item.transform);
        RectTransform playRT = EnsureRect(playGO);
        playRT.anchorMin = new Vector2(0.5f, 0.5f);
        playRT.anchorMax = new Vector2(0.5f, 0.5f);
        playRT.pivot     = new Vector2(0.5f, 0.5f);
        playRT.anchoredPosition = Vector2.zero;
        playRT.sizeDelta = new Vector2(80f, 80f);

        TextMeshProUGUI playTxt = GetOrAddTMP(playGO);
        playTxt.text      = "▶";
        playTxt.fontSize  = 48f;
        playTxt.alignment = TextAlignmentOptions.Center;
        playTxt.color     = new Color(1f, 1f, 1f, 0.85f);

        // ── Bottom bar ────────────────────────────────────────────────────
        GameObject barGO = GetOrCreate("BottomBar", item.transform);
        RectTransform barRT = EnsureRect(barGO);
        barRT.anchorMin = new Vector2(0f, 0f);
        barRT.anchorMax = new Vector2(1f, 0f);
        barRT.pivot     = new Vector2(0.5f, 0f);
        barRT.anchoredPosition = Vector2.zero;
        barRT.sizeDelta = new Vector2(0f, 48f);

        Image barImg = GetOrAddImage(barGO);
        barImg.color = new Color(0f, 0f, 0f, 0.65f);

        // ── Race label ────────────────────────────────────────────────────
        GameObject labelGO = GetOrCreate("RaceLabel", barGO.transform);
        RectTransform labelRT = EnsureRect(labelGO);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10f, 0f);
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTxt = GetOrAddTMP(labelGO);
        labelTxt.text      = race;
        labelTxt.fontSize  = 18f;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.alignment = TextAlignmentOptions.MidlineLeft;
        labelTxt.color     = Color.white;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static GameObject GetOrCreate(string name, Transform parent)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.gameObject;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform EnsureRect(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    static TextMeshProUGUI GetOrAddTMP(GameObject go)
    {
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (t == null) t = go.AddComponent<TextMeshProUGUI>();
        return t;
    }

    static Image GetOrAddImage(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        return img;
    }
}
