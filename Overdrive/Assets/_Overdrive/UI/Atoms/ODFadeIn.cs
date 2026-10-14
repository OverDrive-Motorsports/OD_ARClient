/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODFadeIn - Fades its object in (CanvasGroup alpha, ease-out) when enabled
 ## or when Play is called, e.g. when a list switches to other content.
 ## Alpha only, so it is safe under layout groups.
 ##
 */

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ODFadeIn : MonoBehaviour
{
    public float duration = .22f;

    CanvasGroup _group;
    float _t = 1f;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable() => Play();

    public void Play()
    {
        if (_group == null) Awake();
        _t = 0f;
        _group.alpha = 0f;
    }

    void Update()
    {
        if (_t >= 1f) return;
        _t = Mathf.Min(1f, _t + ODTime.Delta / Mathf.Max(.01f, duration));
        _group.alpha = ODRuntimeUI.EaseOutCubic(_t);
    }
}
