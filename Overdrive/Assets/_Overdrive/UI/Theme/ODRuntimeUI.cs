/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODRuntimeUI - Small builders for UI created at runtime in the visionOS
 ## style (top-left anchored rects, rounded fills, Inter labels). Same visual
 ## vocabulary as CategoryPagesSetup, which is Editor-only.
 ##
 */

using TMPro;
using UnityEngine;

public static class ODRuntimeUI
{
    public static readonly Color Secondary = new Color(1f, 1f, 1f, .62f);
    public static readonly Color Tertiary = new Color(1f, 1f, 1f, .42f);
    public static readonly Color Line = new Color(1f, 1f, 1f, .10f);
    public static readonly Color Gold = new Color(.93f, .78f, .45f);
    public static readonly Color Dark = new Color(.08f, .10f, .15f);
    public static readonly Color LiveRed = new Color(1f, .34f, .28f);

    /// <summary>Rect anchored top-left of its parent; x/y are distances from that corner.</summary>
    public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    /// <summary>Rect stretched over its whole parent.</summary>
    public static RectTransform Stretch(Transform parent, string name)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static RoundedImage Fill(Transform parent, string name, float x, float y, float w, float h, Color color, float radius)
    {
        var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<RoundedImage>();
        image.color = color;
        image.cornerRadius = radius;
        image.cornerSegments = 16;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>Inter label: bold = SemiBold (Bold from 30 pt up), else Regular.</summary>
    public static TextMeshProUGUI Text(Transform parent, string name, string text, float x, float y, float w, float h,
        float size, bool bold = false, bool centered = false, Color? color = null)
    {
        var label = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        label.color = color ?? Color.white;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        ODChampionshipTheme.SetFont(label, bold);
        return label;
    }

    /// <summary>Deactivated first so layout groups ignore children until Destroy runs at end of frame.</summary>
    public static void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Object.Destroy(child);
        }
    }

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
