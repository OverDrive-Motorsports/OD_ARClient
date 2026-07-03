/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## UITheme - Centralized ScriptableObject holding all brand colors, typography sizes, and design tokens.
 ##
 */

using UnityEngine;

/// <summary>
/// Singleton ScriptableObject that drives every visual value in the OD_UI system.
/// Resolved via <see cref="Instance"/> which loads from Resources/UITheme.asset at runtime.
/// Mirrors the brand palette defined in the Flutter app_theme.dart.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "OD_UI/Theme")]
public class UITheme : ScriptableObject
{
    private static UITheme _instance;

    /// <summary>
    /// Auto-resolved singleton. Loads from Resources/UITheme.asset on first access.
    /// Returns null when no UITheme asset exists in Resources — all consumers must null-check.
    /// </summary>
    public static UITheme Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<UITheme>("UITheme");
            return _instance;
        }
    }

    [Header("Backgrounds")]
    /// <summary>Primary frosted dark panel — rgba(28,28,32, 0.78). Used by ODBackground.Card.</summary>
    public Color panelBackground    = new Color(0.110f, 0.110f, 0.125f, 0.78f);
    /// <summary>Elevated panel variant — rgba(36,36,42, 0.85). Used by ODBackground.Alt.</summary>
    public Color panelBackgroundAlt = new Color(0.141f, 0.141f, 0.165f, 0.85f);
    /// <summary>Modal / deep surface — rgba(44,44,52, 0.90). Used by ODBackground.Modal.</summary>
    public Color surfaceColor       = new Color(0.173f, 0.173f, 0.204f, 0.90f);
    /// <summary>Low-emphasis overlay — rgba(20,20,24, 0.50). Used by ODBackground.Subtle.</summary>
    public Color subtleColor        = new Color(0.078f, 0.078f, 0.094f, 0.50f);

    [Header("Text")]
    /// <summary>Near-white primary text. #F2F2F7.</summary>
    public Color textPrimary   = new Color(0.949f, 0.949f, 0.969f, 1.00f);
    /// <summary>Secondary and placeholder text. #8E8E93.</summary>
    public Color textSecondary = new Color(0.557f, 0.557f, 0.576f, 1.00f);
    /// <summary>De-emphasized tertiary text. #48484A.</summary>
    public Color textTertiary  = new Color(0.282f, 0.282f, 0.290f, 1.00f);

    [Header("Brand — shared with Flutter app_theme.dart")]
    /// <summary>Championship gold accent. #C9A84C.</summary>
    public Color accentGold   = new Color(0.788f, 0.659f, 0.298f, 1.00f);
    /// <summary>Interactive blue. #0A84FF.</summary>
    public Color blueColor    = new Color(0.039f, 0.518f, 1.000f, 1.00f);
    /// <summary>Danger / penalty red. #E8002D.</summary>
    public Color dangerColor  = new Color(0.910f, 0.000f, 0.176f, 1.00f);
    /// <summary>Positive / safe green. #32D74B.</summary>
    public Color successColor = new Color(0.196f, 0.843f, 0.294f, 1.00f);

    [Header("Borders")]
    /// <summary>Subtle white border at 10% opacity — separators and ghost button strokes.</summary>
    public Color borderColor      = new Color(1.000f, 1.000f, 1.000f, 0.10f);
    /// <summary>Gold border bright end (#C9A84C at 60%) — top-left of ODGoldBorder diagonal gradient.</summary>
    public Color goldBorderColorA = new Color(0.788f, 0.659f, 0.298f, 0.60f);
    /// <summary>Gold border dim end (#C9A84C at 8%) — bottom-right of ODGoldBorder diagonal gradient.</summary>
    public Color goldBorderColorB = new Color(0.788f, 0.659f, 0.298f, 0.08f);

    [Header("Shape")]
    /// <summary>Default rounded corner radius in UI units. Applied to panels and buttons.</summary>
    public float cornerRadius    = 24f;
    /// <summary>Default border stroke width in UI units.</summary>
    public float borderWidth     = 1.5f;
    /// <summary>Gold border stroke width used by ODGoldBorder texture generation.</summary>
    public float goldBorderWidth = 1.5f;

    [Header("Blur")]
    /// <summary>Controls blur intensity (0–100). Mapped to pixel radius inside ODBlurBackground.</summary>
    public float panelBlurAmount = 54f;

    [Header("Typography — Font Sizes")]
    /// <summary>Display heading size (36). Used by ODLabel.H1 and driver number text.</summary>
    public float h1Size      = 36f;
    /// <summary>Section heading size (28). Used by ODLabel.H2 and telemetry values.</summary>
    public float h2Size      = 28f;
    /// <summary>Body copy size (22). Used by ODLabel.Body and table cell text.</summary>
    public float bodySize    = 22f;
    /// <summary>Small caption size (18). Used by ODLabel.Caption, badge labels, and column headers.</summary>
    public float captionSize = 18f;

    [Header("Typography — Display Font (future use)")]
    /// <summary>When true, H1 / driver number elements use the font named in displayFontName.</summary>
    public bool   useOrbitronForDisplay = false;
    /// <summary>Name of the display TMP font asset. Must exist in a Resources folder to be loaded.</summary>
    public string displayFontName       = "Orbitron";
}
