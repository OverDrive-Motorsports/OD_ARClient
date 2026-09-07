/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTableColumn - Serializable data class describing a single column's display rules for ODDataTable.
 ##
 */

using UnityEngine;
using TMPro;

/// <summary>
/// Pure data descriptor for a table column — not a MonoBehaviour.
/// Serialize a List&lt;ODTableColumn&gt; in any MonoBehaviour to define the table structure
/// in the Inspector, or construct columns from code when data is dynamic.
/// </summary>
[System.Serializable]
public class ODTableColumn
{
    /// <summary>Unique identifier used for cell lookups by columnId in ODTableRow and ODDataTable.</summary>
    [Tooltip("Unique identifier used for cell lookups.")]
    public string columnId;

    /// <summary>Text shown in the header row of the table.</summary>
    [Tooltip("Text shown in the header row.")]
    public string header;

    /// <summary>Relative width weight for LayoutElement.flexibleWidth. 1 = equal share across all columns.</summary>
    [Tooltip("Relative width weight (flex). 1 = equal share.")]
    public float flexWidth = 1f;

    /// <summary>Horizontal text alignment applied to every cell in this column.</summary>
    [Tooltip("Horizontal text alignment inside each cell.")]
    public TextAlignmentOptions alignment = TextAlignmentOptions.Left;

    /// <summary>When true, cell text in this column is rendered bold.</summary>
    [Tooltip("Render cell text in bold.")]
    public bool bold = false;

    /// <summary>When true, cell text uses UITheme.accentGold instead of the default text color.</summary>
    [Tooltip("Highlight cells in this column with accentGold.")]
    public bool isAccent = false;

    // Unity's serializer cannot handle Nullable<Color> (Color?).
    // The bool+Color pair is the standard workaround for optional serialized color overrides.
    /// <summary>When true, customColor overrides the default cell text color for this column.</summary>
    [Tooltip("When true, customColor overrides the default cell text color.")]
    public bool useCustomColor = false;
    public Color customColor = Color.white;

    /// <summary>Returns the custom color when useCustomColor is set, or null to fall back to theme defaults.</summary>
    public Color? GetCustomColor() => useCustomColor ? (Color?)customColor : null;
}
