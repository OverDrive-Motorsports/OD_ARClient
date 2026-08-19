/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODDriverCard - Organism displaying a driver's number, team branding, and a 2-column telemetry grid.
 ##
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-contained driver information card. Hierarchy (built by ODUIBuilder):
/// Background → GoldBorder → TeamColorBar (left edge) → Header (number / logo / name) → Divider → TelemetryGrid.
/// The telemetry grid is rebuilt from scratch in RebuildTelemetry() when SetTelemetry() is called.
/// </summary>
public class ODDriverCard : MonoBehaviour
{
    /// <summary>Data pair used to populate a single telemetry cell inside this card.</summary>
    [System.Serializable]
    public class TelemetryCellData
    {
        public string label;
        public string value;
    }

    [Header("Content")]
    public string driverNumber = "44";
    public string driverName = "Driver Name";
    public Sprite teamLogo;
    public Color teamColor = Color.white;

    [Header("Telemetry")]
    public List<TelemetryCellData> telemetry = new List<TelemetryCellData>();

    [Header("References — wired by ODUIBuilder")]
    public ODBackground background;
    public ODGoldBorder goldBorder;
    /// <summary>4px left-edge color bar tinted to the team's brand color.</summary>
    public Image teamColorBar;
    public TextMeshProUGUI driverNumberText;
    public Image teamLogoImage;
    public TextMeshProUGUI driverNameText;
    public Image divider;
    /// <summary>GridLayoutGroup (2 columns) that hosts the ODTelemetryCell children.</summary>
    public RectTransform telemetryGrid;

    private void Start()
    {
        Sync();
    }

    /// <summary>Pushes all current field values to child references and rebuilds the telemetry grid.</summary>
    private void Sync()
    {
        UITheme theme = UITheme.Instance;

        if (driverNumberText != null)
        {
            driverNumberText.text = driverNumber;
            driverNumberText.fontStyle = FontStyles.Bold;
            if (theme != null)
            {
                driverNumberText.fontSize = theme.h1Size;
                // Driver number uses gold to make it the visual anchor of the card
                driverNumberText.color = theme.accentGold;
            }
        }

        if (driverNameText != null)
        {
            driverNameText.text = driverName;
            if (theme != null)
            {
                driverNameText.fontSize = theme.bodySize;
                driverNameText.color = theme.textPrimary;
            }
        }

        if (teamLogoImage != null && teamLogo != null)
            teamLogoImage.sprite = teamLogo;

        if (teamColorBar != null)
            teamColorBar.color = teamColor;

        if (divider != null && theme != null)
            divider.color = theme.borderColor;

        RebuildTelemetry();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Updates driver number and name text without rebuilding the entire card.</summary>
    public void SetDriver(string number, string name)
    {
        driverNumber = number;
        driverName = name;
        if (driverNumberText != null) driverNumberText.text = number;
        if (driverNameText != null) driverNameText.text = name;
    }

    /// <summary>Replaces the team logo sprite and applies the team color to the side color bar.</summary>
    public void SetTeam(Sprite logo, Color color)
    {
        teamLogo = logo;
        teamColor = color;
        if (teamLogoImage != null && logo != null) teamLogoImage.sprite = logo;
        if (teamColorBar != null) teamColorBar.color = color;
    }

    /// <summary>Replaces the full telemetry cell list and triggers a grid rebuild.</summary>
    public void SetTelemetry(List<TelemetryCellData> cells)
    {
        telemetry = cells;
        RebuildTelemetry();
    }

    /// <summary>Patches a single telemetry cell by index — useful for live data updates.</summary>
    public void UpdateTelemetryValue(int index, string newValue)
    {
        if (telemetryGrid == null || index < 0 || index >= telemetryGrid.childCount) return;
        var cell = telemetryGrid.GetChild(index).GetComponent<ODTelemetryCell>();
        if (cell != null) cell.SetValue(newValue);
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Destroys all existing grid children then creates one ODTelemetryCell GO per entry in telemetry.
    /// Each cell is a VerticalLayoutGroup containing a label TMP and a value TMP.
    /// </summary>
    private void RebuildTelemetry()
    {
        if (telemetryGrid == null) return;

        for (int i = telemetryGrid.childCount - 1; i >= 0; i--)
            Destroy(telemetryGrid.GetChild(i).gameObject);

        UITheme theme = UITheme.Instance;

        foreach (TelemetryCellData data in telemetry)
        {
            var cellGO = new GameObject("TelCell_" + data.label, typeof(RectTransform));
            cellGO.transform.SetParent(telemetryGrid, false);

            var vg = cellGO.AddComponent<VerticalLayoutGroup>();
            vg.childForceExpandWidth = true;
            vg.childForceExpandHeight = false;
            vg.spacing = 2f;
            vg.padding = new RectOffset(0, 0, 4, 4);

            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(cellGO.transform, false);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = data.label;
            lbl.fontStyle = FontStyles.Normal;
            if (theme != null) { lbl.fontSize = theme.captionSize; lbl.color = theme.textSecondary; }

            var valGO = new GameObject("Value", typeof(RectTransform));
            valGO.transform.SetParent(cellGO.transform, false);
            var val = valGO.AddComponent<TextMeshProUGUI>();
            val.text = data.value;
            val.fontStyle = FontStyles.Bold;
            if (theme != null) { val.fontSize = theme.h2Size; val.color = theme.textPrimary; }

            // Wire references into ODTelemetryCell so SetValue() can animate updates
            var telCell = cellGO.AddComponent<ODTelemetryCell>();
            telCell.labelText = lbl;
            telCell.valueText = val;
            telCell.label = data.label;
            telCell.value = data.value;
        }
    }
}
