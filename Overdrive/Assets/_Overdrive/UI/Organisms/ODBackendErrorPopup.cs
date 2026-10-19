/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBackendErrorPopup - Blocking popup shown over a page when the backend
 ## can't be reached: a message plus "Réessayer" and "Afficher les données de
 ## démo" in the design's Alerte layout (ODDesign). Built at runtime (no
 ## prefab). Display only: the caller decides what each button does.
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ODBackendErrorPopup : MonoBehaviour
{
    /// <summary>
    /// Shows (or replaces) the popup covering <paramref name="host"/>. Either
    /// callback closes the popup before running.
    /// </summary>
    public static ODBackendErrorPopup Show(RectTransform host, string title, string message,
        UnityAction onRetry, UnityAction onUseMocks)
    {
        Hide(host);

        var root = new GameObject("BackendErrorPopup", typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        rect.SetParent(host, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        var popup = root.AddComponent<ODBackendErrorPopup>();
        popup.Build(title, message, onRetry, onUseMocks);
        return popup;
    }

    public static void Hide(RectTransform host)
    {
        if (host == null) return;
        var existing = host.GetComponentsInChildren<ODBackendErrorPopup>(true);
        foreach (var popup in existing) Destroy(popup.gameObject);
    }

    /// <summary>
    /// Design "Alerte" (Canva 04): 800 x 560 card, radius 56, tinted icon disc,
    /// centred title and message, then the main action (white) above the
    /// secondary one (white 14 %), both 72-px capsules across the card.
    /// </summary>
    void Build(string title, string message, UnityAction onRetry, UnityAction onUseMocks)
    {
        // Full-size overlay: dims the page and swallows every pointer hit behind the card.
        var overlay = gameObject.AddComponent<RoundedImage>();
        overlay.color = new Color(0f, 0f, 0f, .30f);
        overlay.cornerRadius = ODDesign.Px(ODDesign.WindowRadius);
        overlay.raycastTarget = true;

        const float cw = 800f, ch = 560f;
        var card = Centered(transform, "Card", ODDesign.Px(cw), ODDesign.Px(ch));
        var surface = card.gameObject.AddComponent<ODHomepageSurface>();
        surface.color = Color.white;
        surface.cornerRadius = ODDesign.Px(56f);
        surface.topLeft = ODDesign.WithAlpha(ODDesign.Hex("34373F"), .97f);
        surface.bottomRight = ODDesign.WithAlpha(ODDesign.Hex("25272D"), .97f);
        surface.raycastTarget = true;
        surface.glass = true;
        surface.sheen = 0f;
        surface.shadowSize = 40f;
        surface.shadowAlpha = .45f;
        surface.rimWidth = 1.5f;
        surface.rimTop = new Color(1f, 1f, 1f, .16f);
        surface.rimBottom = new Color(1f, 1f, 1f, .06f);

        // Icon colour follows the kind of problem: connection (red), no data (orange), session (blue).
        string t = (title ?? "").ToLowerInvariant();
        bool session = t.Contains("401") || t.Contains("refus") || t.Contains("session") || t.Contains("accès");
        bool empty = t.Contains("aucune") || t.Contains("indisponible");
        Color tint = session ? ODDesign.WithAlpha(ODDesign.Blue, .26f) : empty ? ODDesign.WithAlpha(ODDesign.Orange, .22f) : ODDesign.WithAlpha(ODDesign.Live, .22f);
        Color icon = session ? ODDesign.BlueSoft : empty ? ODDesign.OrangeText : ODDesign.Hex("FF7A70");

        var disc = ODDesign.Fill(card, "Icon", (cw - 88f) * .5f, 46f, 88f, 88f, tint, 44f);
        ODDesign.Text(disc.transform, "Glyph", "!", 0f, 0f, 88f, 88f, 46f, ODDesign.Weight.Bold, icon, TextAlignmentOptions.Center);
        ODDesign.Text(card, "Title", title, 56f, 46f + 88f + 26f, cw - 112f, 50f, 38f, ODDesign.Weight.Bold, ODDesign.Primary, TextAlignmentOptions.Center);
        var body = ODDesign.Text(card, "Message", message, (cw - 640f) * .5f, 46f + 88f + 26f + 50f + 14f, 640f, 110f, 23f,
            ODDesign.Weight.Regular, new Color(1f, 1f, 1f, .72f), TextAlignmentOptions.Top);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;

        var retry = ODDesign.Button(card, "RetryButton", "Réessayer", 56f, ch - 44f - 72f * 2f - 12f, cw - 112f, 72f, ODDesign.ButtonKind.Primary);
        retry.onClick.AddListener(() => { Destroy(gameObject); onRetry?.Invoke(); });
        var mocks = ODDesign.Button(card, "MockButton", "Afficher les données de démo", 56f, ch - 44f - 72f, cw - 112f, 72f, ODDesign.ButtonKind.Secondary);
        mocks.onClick.AddListener(() => { Destroy(gameObject); onUseMocks?.Invoke(); });
    }

    static RectTransform Centered(Transform parent, string name, float w, float h)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

}
