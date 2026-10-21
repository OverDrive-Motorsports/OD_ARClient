/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODProfileView - Profil / Réglages window (Canva pages 02.9 and 02.10).
 ## Profil: identity, headset, activity, favourites, saved spaces, data
 ## providers, account actions. Réglages: three columns — experience and
 ## notifications, comfort and interaction, display and audio — saved
 ## automatically. Mock-up only: the values are placeholders and every
 ## control (switches, segmented controls, sliders, buttons) animates locally
 ## without being connected to anything. Built at runtime, design px (ODDesign).
 ##
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using D = ODDesign;

public class ODProfileView : MonoBehaviour
{
    const float X0 = D.WindowPadSide, W = 1920f - D.WindowPadSide * 2f, Top = D.WindowPadTop;
    const float HeaderBottom = 162f, Bottom = 1200f - D.WindowPadBottom;

    TextMeshProUGUI _title;
    RectTransform _profileTab, _settingsTab;
    bool _built;

    public void Build()
    {
        if (_built) return;
        _built = true;
        var root = (RectTransform)transform;

        var surface = GetComponent<ODHomepageSurface>();
        if (surface == null) surface = gameObject.AddComponent<ODHomepageSurface>();
        surface.color = Color.white;
        surface.raycastTarget = true;   // the window swallows rays like the other pages
        ODChampionshipTheme.ApplyBackground(surface, ODChampionshipTheme.NeutralPalette);

        D.Caps(root, "Caps", "Compte", X0, Top, 600f, D.PageCaps);
        _title = D.Text(root, "Heading", "Profil", X0, Top + 37f, 900f, 72f, D.TitleXL, D.Weight.Bold);

        // Profil / Réglages: the "mode" segmented control (solid white pill, dark text).
        float switchW = 330f;
        var tabs = ODSegmentedControl.Create(root, D.Px(1920f - X0 - switchW), D.Px(HeaderBottom - 72f), D.Px(switchW), D.Px(72f),
            new[] { "Profil", "Réglages" }, 0, D.Px(23f), solidPill: true);
        tabs.Changed += ShowTab;

        _profileTab = ODRuntimeUI.Stretch(root, "ProfileTab");
        _profileTab.gameObject.AddComponent<ODFadeIn>();
        BuildProfile(_profileTab);

        _settingsTab = ODRuntimeUI.Stretch(root, "SettingsTab");
        _settingsTab.gameObject.AddComponent<ODFadeIn>();
        BuildSettings(_settingsTab);
        _settingsTab.gameObject.SetActive(false);
    }

    void ShowTab(int index)
    {
        _title.text = index == 0 ? "Profil" : "Réglages";
        _profileTab.gameObject.SetActive(index == 0);    // ODFadeIn plays on enable
        _settingsTab.gameObject.SetActive(index == 1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Profil
    // ═══════════════════════════════════════════════════════════════════════

    void BuildProfile(RectTransform tab)
    {
        // Identity strip.
        float y = HeaderBottom + 36f;
        var identity = Card(tab, "Identity", X0, y, W, 150f, D.CardRadius);
        var avatar = D.Fill(identity, "Avatar", 40f, 27f, 96f, 96f, D.Hex("C9A84C"), 48f);
        D.Text(avatar.transform, "Initials", "CA", 0f, 0f, 96f, 96f, 34f, D.Weight.Bold, D.Hex("2A2008"), TextAlignmentOptions.Center);
        D.Text(identity, "Name", "ClemAlex", 164f, 34f, 1200f, 46f, 36f, D.Weight.Bold);
        D.Text(identity, "Since", "Membre depuis mars 2026 · Compte OverDrive", 164f, 84f, 1200f, 30f, 21f, D.Weight.Regular, Grey(.64f));

        y += 150f + 36f;
        float leftW = 820f, rightX = X0 + leftW + 32f, rightW = W - leftW - 32f;

        // Left column: headset, activity, favourites.
        var headset = Card(tab, "Headset", X0, y, leftW, 218f);
        StatusPill(headset, 34f, 28f, "Connecté", D.Green, D.GreenText);
        string[,] specs = { { "Batterie", "68 %" }, { "Stockage", "142 / 256 Go" }, { "Latence flux", "18 ms" }, { "Logiciel", "À jour" } };
        float tileW = (leftW - 68f - 14f * 3f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            var tile = D.Fill(headset, "Spec" + i, 34f + i * (tileW + 14f), 98f, tileW, 92f, new Color(0f, 0f, 0f, .18f), 24f);
            D.Text(tile.transform, "Label", specs[i, 0], 18f, 16f, tileW - 30f, 24f, 17f, D.Weight.Regular, D.Secondary);
            D.Text(tile.transform, "Value", specs[i, 1], 18f, 44f, tileW - 24f, 36f, 26f, D.Weight.SemiBold).overflowMode = TextOverflowModes.Overflow;
        }

        float ay = y + 218f + 28f;
        var activity = Card(tab, "Activity", X0, ay, leftW, 176f);
        D.Caps(activity, "Caps", "Activité", 34f, 28f, 400f, 18f);
        string[,] stats = { { "6 h 20", "regardées · 7 jours" }, { "9", "courses ce mois-ci" }, { "4", "week-ends d’affilée" } };
        float statW = (leftW - 68f - 40f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            D.Text(activity, "Stat" + i, stats[i, 0], 34f + i * (statW + 20f), 72f, statW, 50f, 44f, D.Weight.SemiBold, i == 2 ? D.GoldText : D.Primary);
            D.Text(activity, "StatLabel" + i, stats[i, 1], 34f + i * (statW + 20f), 126f, statW, 26f, 19f, D.Weight.Regular, Grey(.64f));
        }

        float fy = ay + 176f + 28f;
        var favourites = Card(tab, "Favourites", X0, fy, leftW, Bottom - fy);
        D.Caps(favourites, "Caps", "Favoris", 34f, 28f, 400f, 18f);
        string[] codes = { "F1", "WEC", "MGP" }, ids = { "f1", "wec", "motogp" };
        for (int i = 0; i < codes.Length; i++)
        {
            D.Championship(ids[i], out Color colour, out Color dark);
            var badge = D.Fill(favourites, "Fav" + codes[i], 34f + i * 102f, 72f, 84f, 84f, colour, 28f);
            Gradient(badge, colour, Color.Lerp(dark, colour, .35f));
            D.Text(badge.transform, "Code", codes[i], 0f, 0f, 84f, 84f, codes[i].Length > 2 ? 22f : 26f, D.Weight.Bold, Color.white, TextAlignmentOptions.Center);
        }

        // Right column: saved spaces, providers, actions.
        var spaces = Card(tab, "Spaces", rightX, y, rightW, 24f + 84f * 3f + 12f);
        string[,] rows = { { "Poste analyste", "TV, classement, 6 widgets télémétrie", "Actif" }, { "Mode simplifié", "Classement, écarts et faits de course", "" }, { "Soirée endurance", "TV, carte du circuit, relais", "" } };
        for (int i = 0; i < 3; i++)
        {
            float ry = 24f + i * 84f;
            var row = D.Button(spaces, "Space" + i, "", 20f, ry + 6f, rightW - 40f, 72f, D.ButtonKind.Ghost);
            ((RoundedImage)row.targetGraphic).cornerRadius = D.Px(22f);
            D.Text(spaces, "SpaceName" + i, rows[i, 0], 34f, ry + 12f, rightW - 200f, 32f, 24f, D.Weight.SemiBold);
            D.Text(spaces, "SpaceDesc" + i, rows[i, 1], 34f, ry + 46f, rightW - 200f, 26f, 18f, D.Weight.Regular, D.Secondary);
            if (rows[i, 2].Length > 0)
                D.Text(spaces, "SpaceTag" + i, rows[i, 2], rightW - 34f - 120f, ry, 120f, 84f, 18f, D.Weight.SemiBold, D.BlueSoft, TextAlignmentOptions.MidlineRight);
            if (i < 2) D.Line(spaces, 34f, ry + 84f - 1.6f, rightW - 68f);
        }

        float py = y + 24f + 84f * 3f + 12f + 28f;
        var providers = Card(tab, "Providers", rightX, py, rightW, 24f + 30f + 84f * 2f + 12f);
        D.Caps(providers, "Caps", "Fournisseurs", 34f, 24f, 400f, 18f);
        var meta = D.Fill(providers, "MetaIcon", 34f, 54f + 16f, 52f, 52f, D.WithAlpha(D.Green, .18f), 16f);
        D.Text(meta.transform, "Initial", "M", 0f, 0f, 52f, 52f, 24f, D.Weight.Bold, D.GreenText, TextAlignmentOptions.Center);
        D.Text(providers, "MetaName", "Compte Meta", 104f, 54f + 12f, rightW - 300f, 32f, 24f, D.Weight.SemiBold);
        D.Text(providers, "MetaDesc", "Lié automatiquement au casque", 104f, 54f + 46f, rightW - 300f, 26f, 18f, D.Weight.Regular, D.Secondary);
        D.Text(providers, "MetaState", "Connecté", rightW - 34f - 160f, 54f, 160f, 84f, 18f, D.Weight.SemiBold, D.GreenText, TextAlignmentOptions.MidlineRight);
        D.Line(providers, 34f, 54f + 84f - 1.6f, rightW - 68f);
        var add = D.Button(providers, "AddProvider", "", 20f, 54f + 84f + 6f, rightW - 40f, 72f, D.ButtonKind.Ghost);
        ((RoundedImage)add.targetGraphic).cornerRadius = D.Px(22f);
        D.Text(providers, "AddLabel", "Ajouter un fournisseur de données", 34f, 54f + 84f, rightW - 120f, 84f, 24f, D.Weight.SemiBold);
        D.Text(providers, "AddChevron", "›", rightW - 34f - 40f, 54f + 84f, 40f, 84f, 34f, D.Weight.SemiBold, D.Secondary, TextAlignmentOptions.MidlineRight);

        float bw = (rightW - 32f) / 3f;
        string[] actions = { "Modifier le profil", "Se déconnecter", "Supprimer le compte" };
        for (int i = 0; i < 3; i++)
            D.Button(tab, "Action" + i, actions[i], rightX + i * (bw + 16f), Bottom - 72f, bw, 72f,
                i == 2 ? D.ButtonKind.Destructive : D.ButtonKind.Secondary, 22f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Réglages
    // ═══════════════════════════════════════════════════════════════════════

    void BuildSettings(RectTransform tab)
    {
        float y = HeaderBottom + 36f, footerY = Bottom - 64f, colBottom = footerY - 36f;
        float colW = (W - 64f) / 3f;
        float x1 = X0, x2 = X0 + colW + 32f, x3 = X0 + (colW + 32f) * 2f;

        // Column 1: experience, then notifications.
        var experience = Card(tab, "Experience", x1, y, colW, 410f);
        D.Caps(experience, "Caps", "Expérience", 32f, 26f, 400f, 18f);
        float cy = 26f + 25f + 14f;
        cy = SwitchRow(experience, cy, colW, "Mode simplifié", "Classement, écarts et faits de course uniquement", false, 84f);
        cy = SegmentRow(experience, cy, colW, "Unités", new[] { "km/h · °C", "mph · °F" }, 0, true);
        SegmentRow(experience, cy, colW, "Commentaires", new[] { "Français", "Anglais", "Ambiance" }, 0, false);

        float ny = y + 410f + 28f;
        var notifications = Card(tab, "Notifications", x1, ny, colW, colBottom - ny);
        D.Caps(notifications, "Caps", "Notifications", 32f, 26f, 400f, 18f);
        float rowH = (colBottom - ny - 26f - 25f - 4f - 12f) / 3f;
        float ry = 26f + 25f + 4f;
        ry = SwitchRow(notifications, ry, colW, "Départ des courses", "15 min avant, pour tes favoris", true, rowH);
        ry = SwitchRow(notifications, ry, colW, "Résultats", "Podium et classement à l’arrivée", true, rowH);
        SwitchRow(notifications, ry, colW, "Faits de course", "Pénalités, abandons, drapeaux", false, rowH, last: true);

        // Column 2: comfort & interaction.
        var comfort = Card(tab, "Comfort", x2, y, colW, colBottom - y);
        D.Caps(comfort, "Caps", "Confort & interaction", 32f, 26f, 500f, 18f);
        cy = 26f + 25f + 14f;
        cy = SwitchRow(comfort, cy, colW, "Vignette de confort", "Réduit le champ de vision en mouvement", true, 88f);
        cy = SegmentRow(comfort, cy, colW, "Rotation", new[] { "Fluide", "Par paliers" }, 1, true);
        cy = SegmentRow(comfort, cy, colW, "Main dominante", new[] { "Gauche", "Droite" }, 1, true);
        cy = SwitchRow(comfort, cy, colW, "Suivi des mains", "Pincer pour sélectionner, sans manettes", true, 88f);
        SliderRow(comfort, cy + 10f, colW, "Distance des fenêtres", .5f, v => $"{1f + v:0.0} m".Replace('.', ','));

        // Column 3: display & audio.
        var display = Card(tab, "Display", x3, y, colW, colBottom - y);
        D.Caps(display, "Caps", "Affichage & audio", 32f, 26f, 500f, 18f);
        cy = 26f + 25f + 14f;
        cy = SegmentRow(display, cy, colW, "Environnement", new[] { "Ma pièce", "Paddock virtuel" }, 0, true);
        cy = SegmentRow(display, cy, colW, "Qualité graphique", new[] { "Standard", "Élevée", "Ultra" }, 1, true);
        cy = SegmentRow(display, cy, colW, "Fréquence d’affichage", new[] { "72 Hz", "90 Hz", "120 Hz" }, 1, true);
        cy = SwitchRow(display, cy, colW, "Son spatial", "Le moteur suit la voiture autour de toi", true, 88f);
        SliderRow(display, cy + 10f, colW, "Luminosité des fenêtres", .72f, v => $"{Mathf.RoundToInt(v * 100f)} %");

        // Footer: saved automatically, reset.
        D.Fill(tab, "SavedDot", X0, footerY + 27f, 10f, 10f, D.Green, 5f);
        D.Text(tab, "Saved", "Enregistré automatiquement sur ton compte", X0 + 24f, footerY, 900f, 64f, 21f, D.Weight.Regular, D.Muted);
        D.Button(tab, "Reset", "Restaurer les valeurs par défaut", 1920f - X0 - 460f, footerY, 460f, 64f, D.ButtonKind.Secondary, 21f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rows
    // ═══════════════════════════════════════════════════════════════════════

    static Color Grey(float a) => new Color(1f, 1f, 1f, a);

    /// <summary>Design plateau: white 7 %, radius 40, a 1-px light line on top.</summary>
    static RectTransform Card(Transform parent, string name, float x, float y, float w, float h, float radius = D.PlateauRadius)
    {
        var card = D.Fill(parent, name, x, y, w, h, D.Plateau, radius);
        D.Fill(card.transform, "TopLight", radius * .6f, 0f, w - radius * 1.2f, 1.6f, new Color(1f, 1f, 1f, .08f), 0f);
        return card.rectTransform;
    }

    static void StatusPill(Transform parent, float x, float y, string text, Color dot, Color textColour)
    {
        var measure = D.Text(parent, "Measure", text, 0f, 0f, 400f, 50f, 19f, D.Weight.SemiBold);
        float w = measure.GetPreferredValues(text).x / D.Scale + 20f + 10f + 10f + 20f;
        Destroy(measure.gameObject);
        var pill = D.Fill(parent, "Status", x, y, w, 50f, D.WithAlpha(dot, .18f), 25f);
        D.Fill(pill.transform, "Dot", 20f, 20f, 10f, 10f, dot, 5f);
        D.Text(pill.transform, "Label", text, 40f, 0f, w - 40f, 50f, 19f, D.Weight.SemiBold, textColour);
    }

    /// <summary>Title + description on the left, switch on the right, divider below. Returns the next y.</summary>
    static float SwitchRow(Transform card, float y, float w, string title, string description, bool on, float h, bool last = false)
    {
        float textW = w - 64f - 84f - 18f;
        D.Text(card, title, title, 32f, y + h * .5f - 34f, textW, 32f, 24f, D.Weight.SemiBold);
        var desc = D.Text(card, title + "Desc", description, 32f, y + h * .5f + 2f, textW, 26f, 18f, D.Weight.Regular, D.Secondary);
        desc.overflowMode = TextOverflowModes.Ellipsis;
        ODToggleSwitch.Create(card, D.Px(w - 32f - 84f), D.Px(y + (h - 48f) * .5f), on);
        if (!last) D.Line(card, 32f, y + h - 1.6f, w - 64f);
        return y + h;
    }

    /// <summary>Label then a 60-px segmented control (design "Unités", "Rotation"…). Returns the next y.</summary>
    static float SegmentRow(Transform card, float y, float w, string label, string[] options, int selected, bool divider)
    {
        D.Text(card, label, label, 32f, y + 8f, w - 64f, 30f, 22f, D.Weight.SemiBold);
        ODSegmentedControl.Create(card, D.Px(32f), D.Px(y + 8f + 30f + 12f), D.Px(w - 64f), D.Px(60f), options, selected, D.Px(20f));
        float bottom = y + 8f + 30f + 12f + 60f + 16f;
        if (divider) D.Line(card, 32f, bottom - 1.6f, w - 64f);
        return bottom;
    }

    static void SliderRow(Transform card, float y, float w, string label, float value, Func<float, string> format)
    {
        D.Text(card, label, label, 32f, y, w - 200f, 30f, 22f, D.Weight.SemiBold);
        var readout = D.Text(card, label + "Value", format(value), w - 32f - 160f, y, 160f, 30f, 22f, D.Weight.Regular, D.Muted, TextAlignmentOptions.MidlineRight);
        var track = D.Fill(card, label + "Track", 32f, y + 30f + 16f, w - 64f, 12f, new Color(0f, 0f, 0f, .28f), 6f);
        var slider = track.gameObject.AddComponent<ODMockSlider>();
        slider.Build(track.rectTransform, value, v => readout.text = format(v));
    }

    static void Gradient(RoundedImage image, Color from, Color to)
    {
        // Two stacked fills read as the design's 150° badge gradient at this size.
        image.color = from;
        var shade = D.Fill(image.transform, "Shade", 0f, 0f, image.rectTransform.rect.width / D.Scale, image.rectTransform.rect.height / D.Scale,
            D.WithAlpha(to, .55f), image.cornerRadius / D.Scale);
        shade.transform.SetAsFirstSibling();
    }
}

/// <summary>Mock slider of the design: recessed track, white fill, 34-px knob; drags locally.</summary>
public class ODMockSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    RectTransform _track, _fill, _knob;
    Action<float> _changed;
    float _value;

    public void Build(RectTransform track, float value, Action<float> changed)
    {
        _track = track;
        _changed = changed;
        GetComponent<Graphic>().raycastTarget = true;
        _fill = D.Fill(track, "Fill", 0f, 0f, 10f, 12f, Color.white, 6f).rectTransform;
        _knob = D.Fill(track, "Knob", 0f, -11f, 34f, 34f, Color.white, 17f).rectTransform;
        Set(value);
    }

    void Set(float v)
    {
        _value = Mathf.Clamp01(v);
        float w = _track.rect.width;
        _fill.sizeDelta = new Vector2(Mathf.Max(D.Px(12f), w * _value), _fill.sizeDelta.y);
        _knob.anchoredPosition = new Vector2(w * _value - D.Px(17f), _knob.anchoredPosition.y);
        _changed?.Invoke(_value);
    }

    public void OnPointerDown(PointerEventData e) => Drag(e);
    public void OnDrag(PointerEventData e) => Drag(e);

    void Drag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, e.position, e.pressEventCamera, out Vector2 local))
            Set((local.x - _track.rect.xMin) / _track.rect.width);
    }
}
