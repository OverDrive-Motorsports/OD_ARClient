/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODMapWidget - Molecule placeholder for a future interactive circuit map.
 ##
 */

using UnityEngine;

/// <summary>
/// Placeholder card for the circuit location. Shows a "Carte" label and the
/// location text underneath. Intended to be replaced by an actual
/// interactive map later — SetLocation() is the only thing callers need.
/// </summary>
public class ODMapWidget : MonoBehaviour
{
    [Header("References — wired by builder")]
    public ODBackground background;
    public ODLabel placeholderLabel;
    public ODLabel locationLabel;

    /// <summary>Updates the location line shown under the "Carte" placeholder.</summary>
    public void SetLocation(string location)
    {
        locationLabel?.SetText(location);
    }
}
