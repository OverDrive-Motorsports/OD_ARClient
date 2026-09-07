/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPopup - Reusable confirmation/edit popup wrapping an ODModal. Covers two
 ## generic use cases: a confirmation dialog (message + Cancel/Confirm) and an
 ## edit form (input fields + Cancel/Save).
 ##
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sits on the same root as an ODModal and drives its content area at
/// runtime. Knows nothing about what it's confirming or editing — callers
/// pass in the title/message/fields/labels each time they call Show*().
/// </summary>
public class ODPopup : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    public ODModal modal;

    [Header("Prefab references — wired by ODUIBuilder")]
    public GameObject ghostButtonPrefab;
    public GameObject primaryButtonPrefab;
    public GameObject dangerButtonPrefab;
    public GameObject inputFieldPrefab;

    /// <summary>Message + Cancel/Confirm. Pass danger=true to render the confirm button in the red Danger style.</summary>
    public void ShowConfirm(string title, string message, string confirmLabel, UnityAction onConfirm, bool danger = false)
    {
        modal.SetTitle(title);
        ClearContent();
        AddMessage(message);
        AddButtonRow(confirmLabel, onConfirm, danger);
        modal.Show();
    }

    /// <summary>One input field per label in fieldLabels, then Cancel/Save. onConfirm receives the fields in the same order.</summary>
    public List<ODInputField> ShowForm(string title, List<string> fieldLabels, string confirmLabel, UnityAction<List<ODInputField>> onConfirm)
    {
        modal.SetTitle(title);
        ClearContent();

        var fields = new List<ODInputField>();
        foreach (string label in fieldLabels)
            fields.Add(AddInputField(label));

        AddButtonRow(confirmLabel, () => onConfirm?.Invoke(fields), false);
        modal.Show();
        return fields;
    }

    public void Hide() => modal?.Hide();

    // ── Content builders ─────────────────────────────────────────────────────────

    private void ClearContent()
    {
        if (modal == null || modal.card == null || modal.card.contentArea == null) return;
        for (int i = modal.card.contentArea.childCount - 1; i >= 0; i--)
            Destroy(modal.card.contentArea.GetChild(i).gameObject);
    }

    private void AddMessage(string text)
    {
        GameObject go = new GameObject("Message", typeof(RectTransform));
        go.transform.SetParent(modal.card.contentArea, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel lbl = go.AddComponent<ODLabel>();
        lbl.textStyle = ODLabel.TextStyle.Body;
        go.AddComponent<LayoutElement>().preferredHeight = 48f;
    }

    private ODInputField AddInputField(string label)
    {
        GameObject wrapper = new GameObject(label + "Field", typeof(RectTransform));
        wrapper.transform.SetParent(modal.card.contentArea, false);
        VerticalLayoutGroup vlg = wrapper.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childControlWidth = true;
        wrapper.AddComponent<LayoutElement>().preferredHeight = 74f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(wrapper.transform, false);
        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ODLabel labelLbl = labelGO.AddComponent<ODLabel>();
        labelLbl.textStyle = ODLabel.TextStyle.Caption;
        labelGO.AddComponent<LayoutElement>().preferredHeight = 20f;

        GameObject fieldGO = Instantiate(inputFieldPrefab, wrapper.transform);
        fieldGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);
        fieldGO.AddComponent<LayoutElement>().preferredHeight = 48f;

        return fieldGO.GetComponent<ODInputField>();
    }

    private void AddButtonRow(string confirmLabel, UnityAction onConfirm, bool danger)
    {
        GameObject row = new GameObject("ButtonRow", typeof(RectTransform));
        row.transform.SetParent(modal.card.contentArea, false);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        row.AddComponent<LayoutElement>().preferredHeight = 56f;

        GameObject cancelGO = Instantiate(ghostButtonPrefab, row.transform);
        cancelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 56f);
        ODButton cancelBtn = cancelGO.GetComponent<ODButton>();
        cancelBtn.SetLabel("Annuler");
        cancelBtn.OnClick.AddListener(Hide);

        GameObject confirmAsset = danger ? dangerButtonPrefab : primaryButtonPrefab;
        GameObject confirmGO = Instantiate(confirmAsset, row.transform);
        confirmGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 56f);
        ODButton confirmBtn = confirmGO.GetComponent<ODButton>();
        confirmBtn.SetLabel(confirmLabel);
        confirmBtn.OnClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            Hide();
        });
    }
}
