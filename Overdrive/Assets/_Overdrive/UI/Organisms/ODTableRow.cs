/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTableRow - Single data row in an ODDataTable. Builds TMP cells dynamically from column definitions.
 ##
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages one row of an ODDataTable. Call Setup() after construction to populate TMP cells
/// from a list of ODTableColumn descriptors. The Background and AccentBorder children must be
/// wired externally (either by ODUIBuilder or by ODDataTable.AppendRow).
///
/// RequireComponent ordering matters: HorizontalLayoutGroup must exist before this component
/// is added so [RequireComponent] does not try to add a duplicate HLG.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(HorizontalLayoutGroup))]
public class ODTableRow : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder or ODDataTable")]
    /// <summary>Background Image tinted on highlight or team-color.</summary>
    public Image bgImage;
    /// <summary>Left-edge accent strip shown only when this row is highlighted.</summary>
    public Image accentBorder;

    private readonly List<string>          _columnIds = new List<string>();
    private readonly List<TextMeshProUGUI> _cells     = new List<TextMeshProUGUI>();
    private readonly List<GameObject>      _cellGOs   = new List<GameObject>(); // tracked for cleanup on re-Setup

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Destroys any previously created cells, then instantiates one TMP child per column.
    /// Must be called after the row is parented into the hierarchy so layout is resolved correctly.
    /// </summary>
    public void Setup(List<ODTableColumn> columns, List<string> values)
    {
        // Clear previously created cell GOs before re-populating
        foreach (var go in _cellGOs)
            if (go != null) Destroy(go);
        _cellGOs.Clear();
        _cells.Clear();
        _columnIds.Clear();

        UITheme theme = UITheme.Instance;

        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.spacing                = 0f;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.padding                = new RectOffset(12, 12, 0, 0);
        }

        for (int i = 0; i < columns.Count; i++)
        {
            ODTableColumn col = columns[i];
            string        val = (i < values.Count) ? values[i] : string.Empty;

            var cellGO = new GameObject(col.columnId + "_cell", typeof(RectTransform));
            cellGO.transform.SetParent(transform, false);

            var le           = cellGO.AddComponent<LayoutElement>();
            le.flexibleWidth = col.flexWidth;

            var tmp       = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = val;
            tmp.alignment = col.alignment;

            if (theme != null)
            {
                tmp.fontSize = theme.bodySize;
                if (col.isAccent)
                    tmp.color = theme.accentGold;
                else
                {
                    // Explicit custom color → custom, else default primary text
                    Color? cc = col.GetCustomColor();
                    tmp.color = cc.HasValue ? cc.Value : theme.textPrimary;
                }
            }

            if (col.bold) tmp.fontStyle = FontStyles.Bold;

            _columnIds.Add(col.columnId);
            _cells.Add(tmp);
            _cellGOs.Add(cellGO);
        }
    }

    /// <summary>
    /// Shows the gold accent border and tints the background at 8% gold when highlighted;
    /// clears both when deselected.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (bgImage != null)
        {
            UITheme theme = UITheme.Instance;
            if (highlighted && theme != null)
                bgImage.color = new Color(theme.accentGold.r, theme.accentGold.g, theme.accentGold.b, 0.08f);
            else
                bgImage.color = Color.clear;
        }

        if (accentBorder != null)
            accentBorder.gameObject.SetActive(highlighted);
    }

    /// <summary>Applies a subtle team-color tint (alpha=0.10) to the background — used alongside SetHighlighted.</summary>
    public void SetTint(Color teamColor)
    {
        if (bgImage != null)
            bgImage.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.10f);
    }

    /// <summary>Patches the displayed text in the cell identified by columnId. No-ops when id is not found.</summary>
    public void UpdateCell(string columnId, string newValue)
    {
        int idx = _columnIds.IndexOf(columnId);
        if (idx >= 0 && idx < _cells.Count && _cells[idx] != null)
            _cells[idx].text = newValue;
    }

    /// <summary>Fades the row's CanvasGroup alpha from 0 to 1 over 0.2s, optionally after a stagger delay.</summary>
    public void AnimateIn(float delaySeconds = 0f)
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn(delaySeconds));
    }

    private IEnumerator FadeIn(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        const float duration = 0.20f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }
}
