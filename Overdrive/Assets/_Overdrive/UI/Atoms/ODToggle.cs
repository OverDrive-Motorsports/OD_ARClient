/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODToggle - Simple on/off pill switch atom, for settings-style rows.
 ##
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Themed on/off switch: a pill track with a sliding knob. Entirely
/// code-built via Create() — same runtime-construction convention as
/// ODStandingsWidget/ODMenuOverlay items, no dedicated prefab asset.
/// </summary>
[RequireComponent(typeof(Button))]
public class ODToggle : MonoBehaviour
{
    public bool value;
    public UnityEvent<bool> OnValueChanged;

    [Header("References — wired by Create()")]
    public RoundedImage track;
    public RectTransform knob;

    private Button _button;

    /// <summary>Builds a fresh toggle (default off) under parent.</summary>
    public static ODToggle Create(Transform parent)
    {
        GameObject root = new GameObject("ODToggle", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(52f, 30f);

        RoundedImage trackImg = root.AddComponent<RoundedImage>();
        trackImg.cornerRadius = 15f;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = trackImg;

        GameObject knobGO = new GameObject("Knob", typeof(RectTransform));
        knobGO.transform.SetParent(root.transform, false);
        RectTransform knobRT = knobGO.GetComponent<RectTransform>();
        knobRT.anchorMin = new Vector2(0f, 0.5f);
        knobRT.anchorMax = new Vector2(0f, 0.5f);
        knobRT.pivot     = new Vector2(0.5f, 0.5f);
        knobRT.sizeDelta = new Vector2(24f, 24f);
        RoundedImage knobImg = knobGO.AddComponent<RoundedImage>();
        knobImg.color        = Color.white;
        knobImg.cornerRadius = 12f;
        knobImg.raycastTarget = false;

        ODToggle toggle = root.AddComponent<ODToggle>();
        toggle.track = trackImg;
        toggle.knob  = knobRT;
        toggle._button = button;
        toggle._button.onClick.AddListener(toggle.Toggle);
        toggle.Apply();

        return toggle;
    }

    public void SetValue(bool v, bool notify = true)
    {
        value = v;
        Apply();
        if (notify) OnValueChanged?.Invoke(value);
    }

    private void Toggle() => SetValue(!value);

    private void Apply()
    {
        UITheme theme = UITheme.Instance;
        Color onColor  = theme != null ? theme.accentGold : Color.yellow;
        Color offColor = new Color(1f, 1f, 1f, 0.12f);

        if (track != null) track.color = value ? onColor : offColor;
        if (knob  != null) knob.anchoredPosition = new Vector2(value ? 12f : -12f, 0f);
    }
}
