/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## UITransitions - Shared fade+scale reveal/dismiss coroutines, extracted from
 ## ODModal so every "a new page/panel appears" interaction in the app uses the
 ## same Apple-like easing instead of an instant SetActive cut.
 ##
 */

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Callers own the coroutine (StartCoroutine(UITransitions.FadeScaleIn(...))) —
/// this class only supplies the easing math, it is not itself a MonoBehaviour.
/// </summary>
public static class UITransitions
{
    public const float DefaultInDuration = 0.20f;
    public const float DefaultOutDuration = 0.15f;
    public const float DefaultFromScale = 0.92f;

    /// <summary>Ease-out cubic: fast start, gentle settle. Scales up from fromScale to 1 while fading alpha in.</summary>
    public static IEnumerator FadeScaleIn(CanvasGroup group, Transform target,
        float duration = DefaultInDuration, float fromScale = DefaultFromScale, Action onComplete = null)
    {
        if (group != null) group.alpha = 0f;
        if (target != null) target.localScale = Vector3.one * fromScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (group != null) group.alpha = ease;
            if (target != null) target.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, ease);
            yield return null;
        }

        if (group != null) group.alpha = 1f;
        if (target != null) target.localScale = Vector3.one;
        onComplete?.Invoke();
    }

    /// <summary>Ease-in quad: slower start, fast exit. Assumes the target starts fully shown (alpha 1, scale 1).</summary>
    public static IEnumerator FadeScaleOut(CanvasGroup group, Transform target,
        float duration = DefaultOutDuration, float toScale = DefaultFromScale, Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t * t;

            if (group != null) group.alpha = 1f - ease;
            if (target != null) target.localScale = Vector3.one * Mathf.Lerp(1f, toScale, ease);
            yield return null;
        }

        if (group != null) group.alpha = 0f;
        onComplete?.Invoke();
    }
}
