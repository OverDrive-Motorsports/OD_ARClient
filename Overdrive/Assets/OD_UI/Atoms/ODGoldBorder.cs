/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODGoldBorder - Atom that programmatically generates a rounded gold gradient border texture at runtime.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates a 128×128 rounded-rect border texture on OnEnable and assigns it to the attached Image.
/// The texture uses an SDF-based pixel classification to draw only the border ring (outer minus inner
/// rounded rect), filled with a diagonal gold gradient from UITheme.goldBorderColorA to goldBorderColorB.
/// Place as a child of the panel it outlines — ExtendBeyondParent() anchors it to fill and
/// extends by <see cref="oversize"/> units on each side so the border sits exactly on the panel edge.
/// </summary>
[RequireComponent(typeof(Image))]
public class ODGoldBorder : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float oversize = 2f;
    /// <summary>Resolution of the generated border texture. Higher = smoother curves, more memory.</summary>
    [SerializeField] private int   texSize  = 128;

    [Header("Animation")]
    /// <summary>When true, the border alpha oscillates (0.3→0.6) on a 3-second sine loop.</summary>
    [SerializeField] private bool animatePulse = false;

    private Image         _image;
    private RectTransform _rt;
    private Texture2D     _tex;

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        _rt    = GetComponent<RectTransform>();

        GenerateTexture();
        ExtendBeyondParent();

        if (animatePulse)
            StartCoroutine(PulseRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        // CPU texture won't be GC'd automatically — must be explicitly destroyed
        if (_tex != null)
            Destroy(_tex);
    }

    // ── Texture generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Rasterizes the border ring into a RGBA32 texture.
    /// Each pixel is inside-outer-rect AND outside-inner-rect → colored; otherwise transparent.
    /// Diagonal gradient t = (x/w + (1-y/h)) / 2 maps top-left → colorA, bottom-right → colorB.
    /// </summary>
    private void GenerateTexture()
    {
        UITheme theme  = UITheme.Instance;
        Color   colorA = theme != null ? theme.goldBorderColorA : new Color(0.788f, 0.659f, 0.298f, 0.60f);
        Color   colorB = theme != null ? theme.goldBorderColorB : new Color(0.788f, 0.659f, 0.298f, 0.08f);
        float   radius = theme != null ? theme.cornerRadius     : 24f;
        float   bw     = theme != null ? theme.goldBorderWidth  : 1.5f;

        int   w        = texSize;
        int   h        = texSize;
        // Scale corner radius and border width from UI units to texture pixel space (100-unit reference)
        float radiusPx  = Mathf.Clamp(radius * (w / 100f), 2f, w * 0.45f);
        float borderPx  = Mathf.Max(2f, bw * (w / 100f));
        float innerR    = Mathf.Max(1f, radiusPx - borderPx);

        if (_tex != null) Destroy(_tex);
        _tex             = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _tex.wrapMode    = TextureWrapMode.Clamp;
        _tex.filterMode  = FilterMode.Bilinear;

        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool insideOuter = IsInsideRoundedRect(x, y, 0f, 0f, w - 1f, h - 1f, radiusPx);
                bool insideInner = IsInsideRoundedRect(x, y, borderPx, borderPx, w - 1f - borderPx, h - 1f - borderPx, innerR);

                if (insideOuter && !insideInner)
                {
                    // Diagonal gradient: top-left bright (colorA), bottom-right dim (colorB)
                    float t = ((float)x / (w - 1) + (1f - (float)y / (h - 1))) * 0.5f;
                    pixels[y * w + x] = Color.Lerp(colorA, colorB, t);
                }
                else
                {
                    pixels[y * w + x] = Color.clear;
                }
            }
        }

        _tex.SetPixels(pixels);
        _tex.Apply();

        Sprite sp        = Sprite.Create(_tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        _image.sprite    = sp;
        _image.color     = Color.white;
        _image.type      = Image.Type.Simple;
        _image.raycastTarget = false;
    }

    /// <summary>
    /// Returns true when (px, py) lies inside the rounded rectangle defined by the corner coordinates.
    /// Corner quadrants are tested with circle distance from the nearest corner center.
    /// </summary>
    private static bool IsInsideRoundedRect(float px, float py,
                                            float x0, float y0, float x1, float y1, float r)
    {
        if (px < x0 || px > x1 || py < y0 || py > y1) return false;

        float cx = -1f, cy = -1f;
        if      (px < x0 + r && py < y0 + r) { cx = x0 + r; cy = y0 + r; }
        else if (px > x1 - r && py < y0 + r) { cx = x1 - r; cy = y0 + r; }
        else if (px < x0 + r && py > y1 - r) { cx = x0 + r; cy = y1 - r; }
        else if (px > x1 - r && py > y1 - r) { cx = x1 - r; cy = y1 - r; }

        if (cx >= 0f)
        {
            float dx = px - cx, dy = py - cy;
            return (dx * dx + dy * dy) <= (r * r);
        }
        return true;
    }

    // ── Layout ────────────────────────────────────────────────────────────────────

    /// <summary>Stretches this RectTransform to cover its parent, then expands by oversize on all sides.</summary>
    private void ExtendBeyondParent()
    {
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.offsetMin = new Vector2(-oversize, -oversize);
        _rt.offsetMax = new Vector2(oversize,  oversize);
    }

    // ── Animation ────────────────────────────────────────────────────────────────

    /// <summary>Oscillates image alpha between 0.3 and 0.6 on a sine curve over a 3-second period.</summary>
    private IEnumerator PulseRoutine()
    {
        const float periodSeconds = 3f;
        const float alphaMin      = 0.3f;
        const float alphaMax      = 0.6f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < periodSeconds)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(alphaMin, alphaMax, Mathf.Sin(elapsed / periodSeconds * Mathf.PI));
                if (_image != null)
                {
                    Color c = _image.color;
                    c.a = a;
                    _image.color = c;
                }
                yield return null;
            }
        }
    }
}
