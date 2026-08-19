/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWeatherWidget - Molecule showing a big weather readout (icon, value, condition).
 ##
 */

using UnityEngine;

/// <summary>
/// Standalone weather card: an icon glyph, a large value (temperature or
/// rain probability), and a short condition line underneath. Purely
/// presentational — callers push data in via SetWeather().
/// </summary>
public class ODWeatherWidget : MonoBehaviour
{
    [Header("References — wired by builder")]
    public ODBackground background;
    public ODLabel iconLabel;
    public ODLabel valueLabel;
    public ODLabel conditionLabel;

    /// <summary>Updates all 3 displayed fields at once. Any null field is left untouched.</summary>
    public void SetWeather(string icon, string value, string condition)
    {
        if (icon != null) iconLabel?.SetText(icon);
        if (value != null) valueLabel?.SetText(value);
        if (condition != null) conditionLabel?.SetText(condition);
    }
}
