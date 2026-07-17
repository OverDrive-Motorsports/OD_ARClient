/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMenuOverlay - Reusable dropdown: a pill trigger button that reveals a
 ## floating list of items below it. Knows nothing about what the items are —
 ## callers push a list in via SetItems().
 ##
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One trigger (label + chevron) toggles a floating item list. Each item can
/// be marked "selected" (highlighted background) and carries its own
/// onClick. Items are built entirely at runtime (like ODStandingsWidget) —
/// only the trigger/overlay shell is baked into the prefab by ODUIBuilder.
/// </summary>
public class ODMenuOverlay : MonoBehaviour
{
    [System.Serializable]
    public class MenuOverlayItem
    {
        public string      label;
        /// <summary>Text/emoji glyph shown before the label — no icon Sprite assets exist in the project yet.</summary>
        public string      iconGlyph;
        public bool         isSelected;
        public UnityAction  onClick;
    }

    [Header("References — wired by ODUIBuilder")]
    public Button      triggerButton;
    public ODLabel      triggerLabel;
    public GameObject   overlayPanel;
    /// <summary>Items are parented directly here (root of overlayPanel) — see ChampionshipPageController's NewCardBlock pattern for why Background is a sibling with ignoreLayout instead of a separate container.</summary>
    public Transform    itemsContainer;

    private CanvasGroup _overlayGroup;
    private Coroutine   _transition;

    private void Awake()
    {
        overlayPanel?.SetActive(false);
        triggerButton?.onClick.AddListener(Toggle);

        if (overlayPanel != null)
        {
            _overlayGroup = overlayPanel.GetComponent<CanvasGroup>();
            if (_overlayGroup == null) _overlayGroup = overlayPanel.AddComponent<CanvasGroup>();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    public void SetTriggerLabel(string label) => triggerLabel?.SetText(label);

    /// <summary>Replaces the dropdown's items and rebuilds their rows.</summary>
    public void SetItems(List<MenuOverlayItem> items)
    {
        if (itemsContainer == null) return;

        for (int i = itemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsContainer.GetChild(i);
            if (child.name != "Background") Destroy(child.gameObject);
        }

        foreach (var item in items)
            BuildItemRow(item);
    }

    public void Toggle()
    {
        if (overlayPanel == null) return;
        if (overlayPanel.activeSelf) Close();
        else                          Open();
    }

    public void Open()
    {
        if (overlayPanel == null) return;
        overlayPanel.SetActive(true);
        // Render above whatever siblings sit after us in the hierarchy.
        overlayPanel.transform.SetAsLastSibling();

        if (_transition != null) StopCoroutine(_transition);
        _transition = StartCoroutine(UITransitions.FadeScaleIn(_overlayGroup, overlayPanel.transform));
    }

    public void Close()
    {
        if (overlayPanel == null) return;
        if (_transition != null) StopCoroutine(_transition);
        _transition = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return UITransitions.FadeScaleOut(_overlayGroup, overlayPanel.transform);
        overlayPanel.SetActive(false);
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    private void BuildItemRow(MenuOverlayItem item)
    {
        GameObject row = new GameObject(item.label, typeof(RectTransform));
        row.transform.SetParent(itemsContainer, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(16, 16, 10, 10);
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        row.AddComponent<LayoutElement>().preferredHeight = 48f;

        Image bg = row.AddComponent<Image>();
        UITheme theme = UITheme.Instance;
        bg.color = item.isSelected ? (theme != null ? theme.hoverOverlay : new Color(1f, 1f, 1f, 0.08f)) : Color.clear;

        Button button = row.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() =>
        {
            item.onClick?.Invoke();
            Close();
        });

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(row.transform, false);
        TextMeshProUGUI iconTmp = iconGO.AddComponent<TextMeshProUGUI>();
        iconTmp.text      = item.iconGlyph;
        iconTmp.alignment = TextAlignmentOptions.Center;
        ODLabel iconLbl = iconGO.AddComponent<ODLabel>();
        iconLbl.textStyle = ODLabel.TextStyle.Body;
        iconGO.AddComponent<LayoutElement>().preferredWidth = 24f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text               = item.label;
        labelTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        labelTmp.enableWordWrapping = false;
        ODLabel labelLbl = labelGO.AddComponent<ODLabel>();
        labelLbl.textStyle = ODLabel.TextStyle.Body;
        labelGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }
}
