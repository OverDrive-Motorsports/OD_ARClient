/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODBlurBackground - Atom that captures a CPU-based blur snapshot of the scene and applies it as a frosted glass effect.
 ##
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Snapshot-based frosted glass effect for World Space Canvas in VR.
/// Works without Shader Graph or compute shaders — CPU box blur on a half-resolution Texture2D.
/// Attach alongside a RawImage; on RefreshBlur() it waits one frame, reads Screen pixels,
/// runs 3 box-blur passes, and assigns the result. Degrades to a flat tint when Camera.main is null.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ODBlurBackground : MonoBehaviour
{
    [Header("Settings")]
    /// <summary>When true, triggers a blur capture automatically every time the GameObject is enabled.</summary>
    public bool autoRefreshOnEnable = true;

    private RawImage _rawImage;
    private Texture2D _blurTex;

    // Fallback tint applied when Camera.main is not available (e.g. Edit mode, no scene camera)
    private static readonly Color FallbackTint = new Color(0.949f, 0.949f, 0.969f, 0.60f);

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        if (autoRefreshOnEnable)
            StartCoroutine(CaptureNextFrame());
    }

    private void OnDestroy()
    {
        // Release the CPU texture to avoid memory leaks — it is not garbage-collected automatically
        if (_blurTex != null)
            Destroy(_blurTex);
    }

    // ── Public API ───────────────────────────────────────────────────────────────

    /// <summary>Triggers a new blur snapshot on the next rendered frame. Safe to call at any time.</summary>
    public void RefreshBlur()
    {
        if (isActiveAndEnabled)
            StartCoroutine(CaptureNextFrame());
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>Defers capture until the current frame is fully rendered so pixels are up to date.</summary>
    private IEnumerator CaptureNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Capture();
    }

    private void Capture()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // Graceful degradation: flat tinted panel so the UI is still readable
            _rawImage.texture = null;
            _rawImage.color = FallbackTint;
            return;
        }

        int pixelRadius = BlurRadius();
        // Half-resolution is sufficient — blur masks fine detail anyway
        int w = Screen.width / 2;
        int h = Screen.height / 2;

        // Reuse existing texture when dimensions haven't changed to avoid per-frame allocations
        if (_blurTex == null || _blurTex.width != w || _blurTex.height != h)
        {
            if (_blurTex != null) Destroy(_blurTex);
            _blurTex = new Texture2D(w, h, TextureFormat.RGB24, false);
        }

        // ReadPixels works on Quest via the framebuffer — no RenderTexture needed
        _blurTex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        _blurTex.Apply();

        // 3 separable box-blur passes accumulate into a smooth Gaussian approximation
        for (int pass = 0; pass < 3; pass++)
            BoxBlur(_blurTex, pixelRadius);

        _blurTex.Apply();

        UITheme theme = UITheme.Instance;
        Color tint = theme != null ? theme.panelBackground : FallbackTint;

        _rawImage.texture = _blurTex;
        _rawImage.color = tint;
    }

    /// <summary>Maps UITheme.panelBlurAmount (0–100) to a pixel radius on the half-res texture.</summary>
    private int BlurRadius()
    {
        UITheme theme = UITheme.Instance;
        float amount = theme != null ? theme.panelBlurAmount : 54f;
        return Mathf.RoundToInt(Mathf.Lerp(4f, 12f, Mathf.Clamp01(amount / 100f)));
    }

    /// <summary>One separable horizontal + vertical box-blur pass on the texture's pixel array.</summary>
    private static void BoxBlur(Texture2D tex, int radius)
    {
        Color32[] src = tex.GetPixels32();
        Color32[] dst = new Color32[src.Length];
        int w = tex.width;
        int h = tex.height;

        // Horizontal pass: accumulate samples left/right, clamp at edges
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int r = 0, g = 0, b = 0, count = 0;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int sx = Mathf.Clamp(x + dx, 0, w - 1);
                    Color32 c = src[y * w + sx];
                    r += c.r; g += c.g; b += c.b;
                    count++;
                }
                dst[y * w + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
            }
        }

        // Vertical pass: runs on horizontal output (dst), result written to tmp then applied
        Color32[] tmp = new Color32[src.Length];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int r = 0, g = 0, b = 0, count = 0;
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int sy = Mathf.Clamp(y + dy, 0, h - 1);
                    Color32 c = dst[sy * w + x];
                    r += c.r; g += c.g; b += c.b;
                    count++;
                }
                tmp[y * w + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
            }
        }

        tex.SetPixels32(tmp);
    }
}
