/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayTile - Replay card of the replay gallery: a glass plateau with the
 ## Grand Prix, one detail line, a star (save to "Mes replays") and a round
 ## play button. No motion at all: gaze only lights the rim, instantly. The
 ## card is one button; the star is its own button on top.
 ##
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using D = ODDesign;

public class ODReplayTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public struct Info
    {
        public string title;     // Grand Prix
        public string detail;    // "Course · 02/03/2024"
        public bool   saved;     // in "Mes replays"
    }

    static readonly Color RimRest = new Color(1f, 1f, 1f, .10f), RimHover = new Color(1f, 1f, 1f, .40f);

    public Button Button { get; private set; }
    public Button SaveButton { get; private set; }

    ODHomepageSurface _surface;

    /// <summary>Card of <paramref name="w"/> x <paramref name="h"/> design px (the grid places it).</summary>
    public static ODReplayTile Card(Transform parent, Info info, float w, float h)
    {
        var rect = D.Rect(parent, "Replay", 0f, 0f, w, h);
        var surface = rect.gameObject.AddComponent<ODHomepageSurface>();
        surface.raycastTarget = true;
        ODChampionshipTheme.ApplyGlass(surface);
        surface.rimWidth = 1.5f;
        surface.rimTop = surface.rimBottom = RimRest;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.transition = Selectable.Transition.None;

        const float pad = 32f, disc = 72f, star = 56f;
        float textW = w - pad * 2f - disc - 12f - star - 14f;
        D.Text(rect, "Title", info.title, pad, h * .5f - 42f, textW, 40f, 28f, D.Weight.SemiBold);
        D.Text(rect, "Detail", info.detail, pad, h * .5f + 6f, textW, 30f, 21f, D.Weight.Regular, D.Muted);

        var play = D.Fill(rect, "Play", w - pad - disc, (h - disc) * .5f, disc, disc, Color.white, disc * .5f);
        play.raycastTarget = false;
        var icon = D.Rect(play.transform, "Icon", (disc - 28f) * .5f + 2f, (disc - 28f) * .5f, 28f, 28f).gameObject.AddComponent<ODNavigationIcon>();
        icon.symbol = ODNavigationIcon.Symbol.Play;
        icon.color = D.Ink;
        icon.raycastTarget = false;

        var save = D.IconButton(rect, "Save", w - pad - disc - 12f - star, (h - star) * .5f, star, ODNavigationIcon.Symbol.Star,
            info.saved ? D.ButtonKind.Gold : D.ButtonKind.Icon, 26f);
        Object.Destroy(save.GetComponent<ODHoverState>());   // no motion on the replay pages
        var colours = save.colors;
        colours.fadeDuration = 0f;
        save.colors = colours;

        var tile = rect.gameObject.AddComponent<ODReplayTile>();
        tile.Button = button;
        tile.SaveButton = save;
        tile._surface = surface;
        return tile;
    }

    public void OnPointerEnter(PointerEventData eventData) => SetRim(RimHover);
    public void OnPointerExit(PointerEventData eventData) => SetRim(RimRest);
    void OnDisable() => SetRim(RimRest);

    void SetRim(Color colour)
    {
        if (_surface == null) return;
        _surface.rimTop = _surface.rimBottom = colour;
        _surface.SetVerticesDirty();
    }
}
