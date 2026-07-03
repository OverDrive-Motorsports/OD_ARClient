/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODNavItem - Single tab item used inside ODNavBar. Shows icon and label, highlights in gold when selected.
 ##
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single navigation tab. ODNavBar manages which item is selected and calls SetSelected()
/// to toggle between accentGold (active) and textSecondary (inactive) tints.
/// Requires a Button so ODNavBar can wire onClick without adding another component.
/// </summary>
[RequireComponent(typeof(Button))]
public class ODNavItem : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    public ODIcon  icon;
    public ODLabel label;

    [Header("Data")]
    public string itemLabel;
    public Sprite itemIcon;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (icon  == null) icon  = GetComponentInChildren<ODIcon>(true);
        if (label == null) label = GetComponentInChildren<ODLabel>(true);
    }

    /// <summary>Assigns the display label and icon sprite for this tab.</summary>
    public void Setup(string labelText, Sprite iconSprite)
    {
        itemLabel = labelText;
        itemIcon  = iconSprite;
        label?.SetText(labelText);
        icon?.SetIcon(iconSprite);
    }

    /// <summary>
    /// Tints both the icon Image and label TextMeshPro with accentGold when selected,
    /// or textSecondary when inactive.
    /// </summary>
    public void SetSelected(bool selected)
    {
        UITheme theme = UITheme.Instance;
        if (theme == null) return;

        Color color = selected ? theme.accentGold : theme.textSecondary;

        Image           iconImage = icon  != null ? icon.GetComponent<Image>()            : null;
        TextMeshProUGUI labelText = label != null ? label.GetComponent<TextMeshProUGUI>() : null;

        if (iconImage  != null) iconImage.color  = color;
        if (labelText  != null) labelText.color  = color;
    }

    /// <summary>Exposes the Button component so ODNavBar can wire onClick without GetComponent.</summary>
    public Button Button => _button;
}
