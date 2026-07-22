/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ProfileSectionController - Drives the Home nav's "Profil" tab content:
 ## switches between the Profil view and the Réglages view via the top-right
 ## ODMenuOverlay, and builds both views' content at runtime.
 ##
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ProfileSectionController : MonoBehaviour
{
    [Header("References — wired by HomeNavBuilder")]
    public ODMenuOverlay  menuOverlay;
    public RectTransform  profileContent;
    public RectTransform  settingsContent;
    public ODPopup        popup;

    [Header("Prefab references — wired by HomeNavBuilder")]
    public GameObject ghostButtonPrefab;
    public GameObject dangerButtonPrefab;
    public GameObject menuOverlayPrefab;

    // Mock account info — replace with real user data once a backend exists.
    private const string MockName  = "Julien Martin";
    private const string MockEmail = "julien.martin@email.com";

    private CanvasGroup _profileGroup;
    private CanvasGroup _settingsGroup;
    private Coroutine   _viewRoutine;

    private void Start()
    {
        if (profileContent  != null) _profileGroup  = GetOrAddCanvasGroup(profileContent.gameObject);
        if (settingsContent != null) _settingsGroup = GetOrAddCanvasGroup(settingsContent.gameObject);

        SetupMenuOverlay();
        BuildProfileContent();
        BuildSettingsContent();
        ShowProfile();
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        return cg != null ? cg : go.AddComponent<CanvasGroup>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // View switching
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetupMenuOverlay()
    {
        if (menuOverlay == null) return;
        menuOverlay.SetTriggerLabel("Profil");
        RefreshMenuItems("Profil");
    }

    public void ShowProfile()
    {
        SwitchView(profileContent?.gameObject, _profileGroup, settingsContent?.gameObject, _settingsGroup);
        menuOverlay?.SetTriggerLabel("Profil");
        RefreshMenuItems("Profil");
    }

    public void ShowSettings()
    {
        SwitchView(settingsContent?.gameObject, _settingsGroup, profileContent?.gameObject, _profileGroup);
        menuOverlay?.SetTriggerLabel("Réglages");
        RefreshMenuItems("Réglages");
    }

    /// <summary>Cross-fades from one tab view to the other instead of an instant SetActive cut. No-op if already showing.</summary>
    private void SwitchView(GameObject show, CanvasGroup showGroup, GameObject hide, CanvasGroup hideGroup)
    {
        if (show != null && show.activeSelf) return;

        if (_viewRoutine != null) StopCoroutine(_viewRoutine);
        _viewRoutine = StartCoroutine(SwitchViewRoutine(show, showGroup, hide, hideGroup));
    }

    private IEnumerator SwitchViewRoutine(GameObject show, CanvasGroup showGroup, GameObject hide, CanvasGroup hideGroup)
    {
        if (hide != null && hide.activeSelf)
        {
            yield return UITransitions.FadeScaleOut(hideGroup, hide.transform);
            hide.SetActive(false);
        }
        if (show != null)
        {
            show.SetActive(true);
            yield return UITransitions.FadeScaleIn(showGroup, show.transform);
        }
    }

    /// <summary>Only Profil/Réglages — no Abonnement entry.</summary>
    private void RefreshMenuItems(string selectedLabel)
    {
        if (menuOverlay == null) return;
        menuOverlay.SetItems(new List<ODMenuOverlay.MenuOverlayItem>
        {
            new ODMenuOverlay.MenuOverlayItem { label = "Profil",   iconGlyph = "👤", isSelected = selectedLabel == "Profil",   onClick = ShowProfile },
            new ODMenuOverlay.MenuOverlayItem { label = "Réglages", iconGlyph = "⚙",  isSelected = selectedLabel == "Réglages", onClick = ShowSettings },
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Profil view
    // ═══════════════════════════════════════════════════════════════════════════

    private void BuildProfileContent()
    {
        if (profileContent == null) return;

        GameObject account = NewHorizontalRow(profileContent, "Account", 16f);
        account.GetComponent<LayoutElement>().preferredHeight = 72f;

        GameObject avatarGO = new GameObject("Avatar", typeof(RectTransform));
        avatarGO.transform.SetParent(account.transform, false);
        avatarGO.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
        RoundedImage avatarImg = avatarGO.AddComponent<RoundedImage>();
        avatarImg.cornerRadius = 32f;
        avatarGO.AddComponent<ODBackground>().backgroundStyle = ODBackground.Style.Alt;

        GameObject avatarGlyphGO = new GameObject("Glyph", typeof(RectTransform));
        avatarGlyphGO.transform.SetParent(avatarGO.transform, false);
        RectTransform glyphRT = avatarGlyphGO.GetComponent<RectTransform>();
        glyphRT.anchorMin = Vector2.zero; glyphRT.anchorMax = Vector2.one;
        glyphRT.offsetMin = Vector2.zero; glyphRT.offsetMax = Vector2.zero;
        TextMeshProUGUI glyphTmp = avatarGlyphGO.AddComponent<TextMeshProUGUI>();
        glyphTmp.text = "👤";
        glyphTmp.alignment = TextAlignmentOptions.Center;
        avatarGlyphGO.AddComponent<ODLabel>().textStyle = ODLabel.TextStyle.H1;

        GameObject infoCol = new GameObject("Info", typeof(RectTransform));
        infoCol.transform.SetParent(account.transform, false);
        VerticalLayoutGroup infoVlg = infoCol.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment    = TextAnchor.MiddleLeft;
        infoVlg.spacing           = 2f;
        infoVlg.childControlWidth = true;
        infoCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        CreateLabel(infoCol.transform, MockName, ODLabel.TextStyle.H2, TextAlignmentOptions.MidlineLeft);
        CreateLabel(infoCol.transform, MockEmail, ODLabel.TextStyle.Caption, TextAlignmentOptions.MidlineLeft);

        CreateActionButton(profileContent, "Modifier le profil", false, () =>
        {
            popup.ShowForm("Modifier le profil", new List<string> { "Nom", "Email", "Mot de passe" }, "Enregistrer",
                fields => Debug.Log("[Profile] Save requested"));
        });

        CreateActionButton(profileContent, "Partager mon profil", false, () =>
            Debug.Log("[Profile] Share requested"));

        CreateActionButton(profileContent, "Se déconnecter", false, () =>
        {
            popup.ShowConfirm("Se déconnecter", "Voulez-vous vraiment vous déconnecter ?", "Se déconnecter",
                () => Debug.Log("[Profile] Logout confirmed"));
        });

        CreateActionButton(profileContent, "Supprimer le compte", true, () =>
        {
            popup.ShowConfirm("Supprimer le compte", "Cette action est irréversible. Toutes vos données seront supprimées.", "Supprimer",
                () => Debug.Log("[Profile] Account deletion confirmed"), danger: true);
        });
    }

    private void CreateActionButton(Transform parent, string label, bool danger, UnityAction onClick)
    {
        GameObject asset = danger ? dangerButtonPrefab : ghostButtonPrefab;
        GameObject go = Instantiate(asset, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
        go.AddComponent<LayoutElement>().preferredHeight = 56f;
        ODButton btn = go.GetComponent<ODButton>();
        btn.SetLabel(label);
        btn.OnClick.AddListener(onClick);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Réglages view
    // ═══════════════════════════════════════════════════════════════════════════

    private void BuildSettingsContent()
    {
        if (settingsContent == null) return;

        CreateToggleRow(settingsContent, "Notifications", true);
        CreateToggleRow(settingsContent, "Sons", true);
        CreateToggleRow(settingsContent, "Vibrations", true);
        CreatePickerRow(settingsContent, "Langue", new[] { "Français", "English" }, 0);
        CreatePickerRow(settingsContent, "Qualité vidéo", new[] { "Auto", "Haute", "Standard" }, 0);
        CreateInfoRow(settingsContent, "Version", "1.0.0");
    }

    private void CreateToggleRow(Transform parent, string label, bool defaultValue)
    {
        GameObject row = NewHorizontalRow(parent, "Row_" + label, 12f);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
        CreateRowLabel(row.transform, label);
        ODToggle toggle = ODToggle.Create(row.transform);
        toggle.SetValue(defaultValue, notify: false);
    }

    private void CreatePickerRow(Transform parent, string label, string[] options, int defaultIndex)
    {
        GameObject row = NewHorizontalRow(parent, "Row_" + label, 12f);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
        CreateRowLabel(row.transform, label);

        GameObject pickerGO = Instantiate(menuOverlayPrefab, row.transform);
        pickerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 44f);
        ODMenuOverlay picker = pickerGO.GetComponent<ODMenuOverlay>();
        picker.SetTriggerLabel(options[defaultIndex]);

        var items = new List<ODMenuOverlay.MenuOverlayItem>();
        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            items.Add(new ODMenuOverlay.MenuOverlayItem
            {
                label      = option,
                iconGlyph  = "",
                isSelected = i == defaultIndex,
                onClick    = () => picker.SetTriggerLabel(option),
            });
        }
        picker.SetItems(items);
    }

    private void CreateInfoRow(Transform parent, string label, string value)
    {
        GameObject row = NewHorizontalRow(parent, "Row_" + label, 12f);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
        CreateRowLabel(row.transform, label);
        CreateLabel(row.transform, value, ODLabel.TextStyle.Caption, TextAlignmentOptions.MidlineRight);
    }

    private void CreateRowLabel(Transform parent, string text)
    {
        GameObject go = CreateLabel(parent, text, ODLabel.TextStyle.Body, TextAlignmentOptions.MidlineLeft);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Low-level helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static GameObject NewHorizontalRow(Transform parent, string name, float spacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = spacing;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        go.AddComponent<LayoutElement>().preferredHeight = 48f;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);
        return go;
    }

    private static GameObject CreateLabel(Transform parent, string text, ODLabel.TextStyle style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.alignment          = alignment;
        tmp.enableWordWrapping = false;
        ODLabel lbl = go.AddComponent<ODLabel>();
        lbl.textStyle = style;
        return go;
    }
}
