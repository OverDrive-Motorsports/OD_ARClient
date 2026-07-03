using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fills the Races grid with the mockup thumbnails from
/// Assets/_Overdrive/DevAssets/Miniature — one card per image, as if real video
/// thumbnails had been fetched from the backend. Each card is clickable
/// and opens the VideoPlayerCanvas. Re-runnable.
/// </summary>
public static class ApplyMockupThumbnails
{
    const string MINIATURE_DIR = "Assets/_Overdrive/DevAssets/Miniature";
    const string GRID_PATH =
        "OverdriveMenuCanvas/OverdriveMenuPanel/Body/ContentArea/GridScrollView/Viewport/GridContent";

    static readonly Color CardBg  = new Color(0.22f, 0.19f, 0.15f, 1f);
    static readonly Color BarBg   = new Color(0.08f, 0.07f, 0.06f, 0.85f);
    static readonly Color Txt     = new Color(0.92f, 0.90f, 0.86f, 1f);

    // Nice display names instead of raw file names
    static readonly string[] RaceNames = {
        "Bahrain GP", "Saudi Arabia GP", "Australia GP", "Japan GP",
        "China GP", "Miami GP", "Monaco GP", "Spain GP",
        "Canada GP", "Austria GP", "Silverstone GP", "Hungary GP",
        "Spa GP", "Monza GP", "Singapore GP"
    };

    [MenuItem("Overdrive/Apply Mockup Thumbnails")]
    public static void Apply()
    {
        var grid = GameObject.Find(GRID_PATH);
        if (grid == null) { Debug.LogError("[Thumbs] GridContent not found"); return; }

        // ── 1. Import all images as sprites ──────────────────────────────────
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { MINIATURE_DIR });
        var sprites = new System.Collections.Generic.List<Sprite>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sprites.Add(sprite);
        }

        if (sprites.Count == 0) { Debug.LogError("[Thumbs] No images in " + MINIATURE_DIR); return; }

        // ── 2. Size the grid to fill the whole viewport width ─────────────────
        // Viewport is 1156 wide → 2 columns: (1156 - 2*8 padding - 16 spacing)/2 = 562
        var layout = grid.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint      = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.cellSize        = new Vector2(562f, 316f);   // 16:9 cells
            layout.spacing         = new Vector2(16f, 16f);
            layout.padding         = new RectOffset(8, 8, 8, 8);
            layout.childAlignment  = TextAnchor.UpperCenter;
        }

        // ── 3. Clear existing grid items ──────────────────────────────────────
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(grid.transform.GetChild(i).gameObject);

        // ── 4. One card per sprite ────────────────────────────────────────────
        for (int i = 0; i < sprites.Count; i++)
        {
            string raceName = i < RaceNames.Length ? RaceNames[i] : "Race " + (i + 1);
            BuildCard(grid.transform, "Item_" + i, sprites[i], raceName);
        }

        EditorUtility.SetDirty(grid);
        Debug.Log($"[Thumbs] {sprites.Count} thumbnail cards created from {MINIATURE_DIR}.");
    }

    // ── card structure (matches the old placeholder cards) ───────────────────
    static void BuildCard(Transform parent, string name, Sprite sprite, string title)
    {
        // root
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var bg = go.AddComponent<Image>();
        bg.color = CardBg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor      = CardBg;
        cb.highlightedColor = new Color(0.32f, 0.28f, 0.22f, 1f);
        cb.pressedColor     = new Color(0.18f, 0.15f, 0.12f, 1f);
        btn.colors = cb;

        var item = go.AddComponent<ContentGridItem>();

        // thumbnail (fills card, slight inset)
        var thumb = new GameObject("Thumbnail");
        thumb.transform.SetParent(go.transform, false);
        var thRT = thumb.AddComponent<RectTransform>();
        thRT.anchorMin = Vector2.zero; thRT.anchorMax = Vector2.one;
        thRT.offsetMin = new Vector2(4, 4); thRT.offsetMax = new Vector2(-4, -4);
        var thImg = thumb.AddComponent<Image>();
        thImg.sprite = sprite;
        thImg.preserveAspect = false; // fill the card like a video thumb
        thImg.raycastTarget = false;

        // play icon overlay
        var play = new GameObject("PlayIcon");
        play.transform.SetParent(go.transform, false);
        var pRT = play.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(56, 56);
        var pTxt = play.AddComponent<TextMeshProUGUI>();
        pTxt.text = "▶"; pTxt.fontSize = 40f;
        pTxt.color = new Color(1f, 1f, 1f, 0.85f);
        pTxt.alignment = TextAlignmentOptions.Center;
        pTxt.raycastTarget = false;

        // bottom bar with race name
        var bar = new GameObject("BottomBar");
        bar.transform.SetParent(go.transform, false);
        var barRT = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0); barRT.anchorMax = new Vector2(1, 0);
        barRT.pivot = new Vector2(0.5f, 0);
        barRT.offsetMin = new Vector2(4, 4); barRT.offsetMax = new Vector2(-4, 52);
        var barImg = bar.AddComponent<Image>();
        barImg.color = BarBg;
        barImg.raycastTarget = false;

        var label = new GameObject("RaceNameText");
        label.transform.SetParent(bar.transform, false);
        var lRT = label.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(18, 0); lRT.offsetMax = new Vector2(-10, 0);
        var lTxt = label.AddComponent<TextMeshProUGUI>();
        lTxt.text = title; lTxt.fontSize = 20f;
        lTxt.fontStyle = FontStyles.Bold;
        lTxt.color = Txt;
        lTxt.alignment = TextAlignmentOptions.MidlineLeft;
        lTxt.enableWordWrapping = false;
        lTxt.overflowMode = TextOverflowModes.Ellipsis;
        lTxt.raycastTarget = false;

        // wire the runtime component
        item.thumbnail  = thImg;
        item.titleLabel = lTxt;
    }
}
