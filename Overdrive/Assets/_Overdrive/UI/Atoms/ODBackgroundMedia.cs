/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBackgroundMedia - Atom that layers a media texture, a blur overlay, and a gradient vignette for video or image backgrounds.
 ##
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three-layer media background stack:
/// MediaLayer (raw video/image) → BlurOverlay (tinted frosted layer) → GradientOverlay (bottom vignette).
/// Supports both Texture2D and RenderTexture as the media source.
/// </summary>
public class ODBackgroundMedia : MonoBehaviour
{
    [Header("References — wired by ODUIBuilder")]
    /// <summary>Bottom layer that displays the actual media texture.</summary>
    public RawImage mediaLayer;
    /// <summary>Middle layer that darkens and optionally blurs the media for legibility.</summary>
    public RawImage blurOverlay;
    /// <summary>Top layer: a gradient Image anchored to the bottom half to improve text readability.</summary>
    public Image gradientOverlay;

    [Header("Media")]
    /// <summary>Texture assigned to the media layer. Set at runtime via SetTexture() or SetRenderTexture().</summary>
    public Texture mediaTexture;

    [Header("Overlay")]
    /// <summary>Alpha of the blur/tint overlay. Higher values obscure more of the media.</summary>
    [Range(0f, 1f)] public float blurOverlayAlpha = 0.65f;

    private void Start()
    {
        Apply();
    }

    /// <summary>Pushes current field values to child visual components.</summary>
    private void Apply()
    {
        if (mediaLayer != null && mediaTexture != null)
            mediaLayer.texture = mediaTexture;

        if (blurOverlay != null)
        {
            Color c = blurOverlay.color;
            c.a = blurOverlayAlpha;
            blurOverlay.color = c;
        }

        if (gradientOverlay != null)
        {
            // Bottom-anchored gradient: opaque black at base → transparent at midpoint for safe text area
            gradientOverlay.color = new Color(0f, 0f, 0f, 0.80f);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Assigns a static Texture2D or Texture to the media layer.</summary>
    public void SetTexture(Texture tex)
    {
        mediaTexture = tex;
        if (mediaLayer != null) mediaLayer.texture = tex;
    }

    /// <summary>Assigns a live RenderTexture (e.g. from a secondary camera) to the media layer.</summary>
    public void SetRenderTexture(RenderTexture rt)
    {
        mediaTexture = rt;
        if (mediaLayer != null) mediaLayer.texture = rt;
    }

    /// <summary>Adjusts the blur/tint overlay opacity without a full Apply() pass.</summary>
    public void SetBlurAlpha(float a)
    {
        blurOverlayAlpha = a;
        if (blurOverlay != null)
        {
            Color c = blurOverlay.color;
            c.a = a;
            blurOverlay.color = c;
        }
    }
}
