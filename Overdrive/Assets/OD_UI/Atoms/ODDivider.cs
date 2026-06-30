/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODDivider - Atom that renders a 2px full-width horizontal separator line using the theme border color.
 ##
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight horizontal separator. Anchors itself to full width at vertical center,
/// sets height to 2px, and applies UITheme.borderColor. Disable raycast to avoid blocking VR input.
/// </summary>
[RequireComponent(typeof(Image))]
public class ODDivider : MonoBehaviour
{
    private void Start()
    {
        Image image = GetComponent<Image>();
        UITheme theme = UITheme.Instance;
        if (theme != null) image.color = theme.borderColor;
        image.raycastTarget = false;

        // When a LayoutElement is present the parent layout group controls sizing —
        // skip the anchor override so layout-driven dividers keep their preferredHeight.
        if (GetComponent<LayoutElement>() == null)
        {
            RectTransform rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 2f);
        }
    }
}
