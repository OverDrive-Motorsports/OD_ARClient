/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODDataTable - Generic modular table organism. Accepts any column/row structure for multi-championship data display.
 ##
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generic scrollable data table organism.
/// Column definitions are set once via SetColumns(); rows are added via SetData() or AppendRow().
/// Individual cells can be patched live via UpdateCell() without rebuilding the whole table — useful
/// for real-time telemetry or timing-tower updates.
///
/// Row identity is maintained by string rowId keys so consumers don't need to track row indices.
/// </summary>
public class ODDataTable : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    public ODBackground background;
    public ODGoldBorder goldBorder;
    /// <summary>RectTransform of the fixed header row (not scrollable).</summary>
    public RectTransform headerRow;
    /// <summary>RectTransform of the scrollable rows container with a VerticalLayoutGroup.</summary>
    public RectTransform rowsContainer;

    private readonly List<ODTableColumn> _columns = new List<ODTableColumn>();
    private readonly Dictionary<string, ODTableRow> _rowMap = new Dictionary<string, ODTableRow>();
    private int _rowCounter = 0;

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Defines the column structure and rebuilds the header row. Must be called before SetData().</summary>
    public void SetColumns(List<ODTableColumn> columns)
    {
        _columns.Clear();
        _columns.AddRange(columns);
        RebuildHeader();
    }

    /// <summary>
    /// Clears all existing data rows and repopulates from the provided 2D list.
    /// Each inner list corresponds to one row, values aligned to the column order.
    /// </summary>
    public void SetData(List<List<string>> data, List<string> rowIds = null)
    {
        ClearData();
        for (int i = 0; i < data.Count; i++)
        {
            string id = (rowIds != null && i < rowIds.Count) ? rowIds[i] : ("row_" + i);
            AppendRow(data[i], id);
        }
    }

    /// <summary>
    /// Appends a single row to the bottom of the table and returns the row ID used.
    /// Background, AccentBorder, HLG and CanvasGroup are created programmatically here
    /// rather than relying on a prefab so tables stay fully data-driven.
    /// </summary>
    public string AppendRow(List<string> values, string rowId = null)
    {
        if (rowsContainer == null) return null;
        if (rowId == null) rowId = "row_" + (_rowCounter++);

        var go = new GameObject(rowId, typeof(RectTransform));
        go.transform.SetParent(rowsContainer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 44f);
        // LayoutElement.preferredHeight lets the parent VLG+CSF chain measure this row correctly
        go.AddComponent<LayoutElement>().preferredHeight = 44f;

        // Background — absolute positioned so it doesn't compete with the HLG cell layout
        var bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(go.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.clear;
        bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

        // AccentBorder — 3px left-edge strip, visible only when row is highlighted
        var abGO = new GameObject("AccentBorder", typeof(RectTransform));
        abGO.transform.SetParent(go.transform, false);
        var abRT = abGO.GetComponent<RectTransform>();
        abRT.anchorMin = new Vector2(0f, 0f);
        abRT.anchorMax = new Vector2(0f, 1f);
        abRT.sizeDelta = new Vector2(3f, 0f);
        abRT.offsetMin = abRT.offsetMax = Vector2.zero;
        var abImg = abGO.AddComponent<Image>();
        UITheme theme = UITheme.Instance;
        abImg.color = theme != null ? theme.accentGold : Color.yellow;
        abGO.AddComponent<LayoutElement>().ignoreLayout = true;
        abGO.SetActive(false);

        // HLG added before ODTableRow so [RequireComponent] on ODTableRow finds it already present
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0f;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(12, 12, 0, 0);

        go.AddComponent<CanvasGroup>();

        var row = go.AddComponent<ODTableRow>();
        row.bgImage = bgImg;
        row.accentBorder = abImg;
        row.Setup(_columns, values);

        _rowMap[rowId] = row;
        return rowId;
    }

    /// <summary>Destroys all data rows. The header row is preserved.</summary>
    public void ClearData()
    {
        foreach (var kvp in _rowMap)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _rowMap.Clear();
        _rowCounter = 0;
    }

    /// <summary>Highlights the row with the given rowId and clears highlight on all others.</summary>
    public void SetHighlightedRow(string rowId)
    {
        foreach (var kvp in _rowMap)
            kvp.Value.SetHighlighted(kvp.Key == rowId);
    }

    /// <summary>Applies a team-color tint to a specific row background.</summary>
    public void SetRowTint(string rowId, Color teamColor)
    {
        if (_rowMap.TryGetValue(rowId, out ODTableRow row))
            row.SetTint(teamColor);
    }

    /// <summary>Patches a single cell without rebuilding the row.</summary>
    public void UpdateCell(string rowId, string columnId, string newValue)
    {
        if (_rowMap.TryGetValue(rowId, out ODTableRow row))
            row.UpdateCell(columnId, newValue);
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>Destroys all header children and recreates one TMP label per column.</summary>
    private void RebuildHeader()
    {
        if (headerRow == null) return;

        for (int i = headerRow.childCount - 1; i >= 0; i--)
            Destroy(headerRow.GetChild(i).gameObject);

        UITheme theme = UITheme.Instance;

        // GetOrAdd pattern: HLG may already be on this RectTransform
        HorizontalLayoutGroup hlg = headerRow.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(12, 12, 0, 0);

        foreach (ODTableColumn col in _columns)
        {
            var cellGO = new GameObject(col.columnId + "_hdr", typeof(RectTransform));
            cellGO.transform.SetParent(headerRow, false);

            cellGO.AddComponent<LayoutElement>().flexibleWidth = col.flexWidth;

            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.text = col.header;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = col.alignment;
            if (theme != null)
            {
                tmp.fontSize = theme.captionSize;
                // Header labels use secondary color to distinguish them visually from data cells
                tmp.color = theme.textSecondary;
            }
        }
    }
}
