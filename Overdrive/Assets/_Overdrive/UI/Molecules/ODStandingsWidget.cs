/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODStandingsWidget - Generic standings widget with a built-in Pilotes/Écuries
 ## toggle. Knows nothing about which championship it's showing — it only
 ## renders whichever ChampionshipStandingTable(s) it's given.
 ##
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One standings "card": an optional category label (e.g. "Hypercar"), a
/// Pilotes/Écuries toggle (shown only when both tables are supplied), and a
/// single ODDataTable whose columns/rows are rebuilt on toggle. Entirely
/// code-built at runtime — no dedicated prefab asset, same convention as
/// RaceRankingManager/ContentGridView.
/// </summary>
public class ODStandingsWidget : MonoBehaviour
{
    public ODLabel   categoryLabel;
    public GameObject toggleRow;
    public ODButton  driversToggleBtn;
    public ODButton  teamsToggleBtn;
    public ODDataTable table;

    private ChampionshipStandingTable? _drivers;
    private ChampionshipStandingTable? _teams;

    /// <summary>Builds a fresh, empty widget under parent. Call Setup() right after to feed it data.</summary>
    public static ODStandingsWidget Create(Transform parent, GameObject ghostButtonPrefab, GameObject dataTablePrefab)
    {
        GameObject root = new GameObject("StandingsWidget", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 8f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        root.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        ODStandingsWidget widget = root.AddComponent<ODStandingsWidget>();

        // Category label
        GameObject catGO = new GameObject("CategoryLabel", typeof(RectTransform));
        catGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI catTmp = catGO.AddComponent<TextMeshProUGUI>();
        catTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        catTmp.enableWordWrapping = false;
        ODLabel catLbl = catGO.AddComponent<ODLabel>();
        catLbl.textStyle = ODLabel.TextStyle.H2;
        catGO.AddComponent<LayoutElement>().preferredHeight = 32f;
        widget.categoryLabel = catLbl;

        // Pilotes/Écuries toggle
        GameObject toggleGO = new GameObject("Toggle", typeof(RectTransform));
        toggleGO.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup hlg = toggleGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = 8f;
        hlg.childAlignment     = TextAnchor.MiddleLeft;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = true;
        toggleGO.AddComponent<LayoutElement>().preferredHeight = 48f;
        widget.toggleRow = toggleGO;

        GameObject driversBtnGO = Object.Instantiate(ghostButtonPrefab, toggleGO.transform);
        driversBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 44f);
        widget.driversToggleBtn = driversBtnGO.GetComponent<ODButton>();
        widget.driversToggleBtn.SetLabel("Pilotes");

        GameObject teamsBtnGO = Object.Instantiate(ghostButtonPrefab, toggleGO.transform);
        teamsBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 44f);
        widget.teamsToggleBtn = teamsBtnGO.GetComponent<ODButton>();
        widget.teamsToggleBtn.SetLabel("Écuries");

        widget.driversToggleBtn.OnClick.AddListener(() => widget.ShowType(StandingType.Drivers));
        widget.teamsToggleBtn.OnClick.AddListener(() => widget.ShowType(StandingType.Teams));

        // Table
        GameObject tableGO = Object.Instantiate(dataTablePrefab, root.transform);
        widget.table = tableGO.GetComponent<ODDataTable>();

        return widget;
    }

    /// <summary>
    /// Feeds the widget its data. Either table may be null — the toggle only
    /// appears when both are present; with just one, that one shows directly.
    /// </summary>
    public void Setup(string category, ChampionshipStandingTable? drivers, ChampionshipStandingTable? teams)
    {
        _drivers = drivers;
        _teams   = teams;

        categoryLabel?.SetText(string.IsNullOrEmpty(category) ? "Classement" : category);

        bool hasBoth = drivers.HasValue && teams.HasValue;
        if (toggleRow != null) toggleRow.SetActive(hasBoth);

        if (drivers.HasValue)      ShowType(StandingType.Drivers);
        else if (teams.HasValue)   ShowType(StandingType.Teams);
    }

    private void ShowType(StandingType type)
    {
        ChampionshipStandingTable? selected = type == StandingType.Drivers ? _drivers : _teams;
        if (!selected.HasValue || table == null) return;

        table.SetColumns(BuildColumns(type));
        table.SetData(BuildRows(selected.Value));

        if (driversToggleBtn != null)
            driversToggleBtn.SetStyle(type == StandingType.Drivers ? ODButton.ButtonStyle.Primary : ODButton.ButtonStyle.Ghost);
        if (teamsToggleBtn != null)
            teamsToggleBtn.SetStyle(type == StandingType.Teams ? ODButton.ButtonStyle.Primary : ODButton.ButtonStyle.Ghost);
    }

    private static List<ODTableColumn> BuildColumns(StandingType type)
    {
        var columns = new List<ODTableColumn>
        {
            new ODTableColumn { columnId = "pos",  header = "POS", flexWidth = 0.5f, bold = true, isAccent = true, alignment = TextAlignmentOptions.Center },
            new ODTableColumn { columnId = "name", header = "NOM", flexWidth = 2.0f, bold = true, alignment = TextAlignmentOptions.Left },
        };
        if (type == StandingType.Drivers)
            columns.Add(new ODTableColumn { columnId = "team", header = "EQUIPE", flexWidth = 1.6f, alignment = TextAlignmentOptions.Left });
        columns.Add(new ODTableColumn { columnId = "pts", header = "PTS", flexWidth = 0.6f, bold = true, alignment = TextAlignmentOptions.Right });
        return columns;
    }

    private static List<List<string>> BuildRows(ChampionshipStandingTable data)
    {
        var rows = new List<List<string>>();
        foreach (var entry in data.entries)
        {
            var row = new List<string> { entry.position.ToString(), entry.name };
            if (data.type == StandingType.Drivers) row.Add(entry.teamName);
            row.Add(entry.points.ToString());
            rows.Add(row);
        }
        return rows;
    }
}
