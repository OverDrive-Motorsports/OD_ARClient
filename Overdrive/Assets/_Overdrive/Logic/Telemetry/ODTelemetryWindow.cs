/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTelemetryWindow - What the "Telemetry" button of the race page (live
 ## and replay states) opens: every telemetry widget, in both formats, on
 ## the left of the race window, at about 0.56 of its scale (SizeRatio) and
 ## facing the same way. One row per widget of Widgets, as in the design:
 ## Small on the left, Large on the right.
 ## It feeds them: each new frame of the feed (ODMockTelemetry for now) is
 ## pushed to every widget with Show(), so the widgets (UI) never reach the
 ## feed themselves. No backdrop: each widget is its own frosted glass card.
 ## The button (again) hides it; leaving the race page hides it too
 ## (ODSportPage).
 ##
 */

using System;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODTelemetryWindow : MonoBehaviour
{
    /// <summary>The widgets shown, one row each, top to bottom. A new widget only needs its type here.</summary>
    static readonly Type[] Widgets =
    {
        typeof(ODSpeedWidget),
        typeof(ODPedalsWidget),
        typeof(ODSectorsWidget),
        typeof(ODLapTimeWidget),
    };

    const float WindowGap = 64f;     // design px between the race window and this one
    /// <summary>The widgets are a secondary window: 3/4 of 3/4 (≈ 0.56) of the race window's scale.</summary>
    const float SizeRatio = .75f * .75f;

    RectTransform _raceWindow;
    ODTelemetryWidget[] _widgets;
    int _version = -1;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>Creates the (hidden) window beside <paramref name="raceWindow"/>.</summary>
    public static ODTelemetryWindow Create(RectTransform raceWindow)
    {
        Vector2 small = SizeOf(WidgetFormat.Small), large = SizeOf(WidgetFormat.Large);
        int rows = Widgets.Length;
        float pitch = large.y + Gap, column2 = small.x + Gap;

        var canvas = ODPointableCanvas.Create("TelemetryWindow", new Vector2(D.Px(column2 + large.x), D.Px(pitch * rows - Gap)));
        var go = canvas.gameObject;
        go.SetActive(false);
        go.transform.localScale = raceWindow.lossyScale * SizeRatio;

        var window = go.AddComponent<ODTelemetryWindow>();
        window._raceWindow = raceWindow;
        window._widgets = new ODTelemetryWidget[rows * 2];
        for (int row = 0; row < rows; row++)
        {
            window._widgets[row * 2] = ODTelemetryWidget.Create(Widgets[row], go.transform, 0f, pitch * row, WidgetFormat.Small);
            window._widgets[row * 2 + 1] = ODTelemetryWidget.Create(Widgets[row], go.transform, column2, pitch * row, WidgetFormat.Large);
        }
        return window;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    /// <summary>Shows the window on the left of the race window, wherever it is now.</summary>
    public void Open()
    {
        PlaceBeside();
        GetComponent<Canvas>().worldCamera = Camera.main;
        gameObject.SetActive(true);
        Feed();
    }

    public void Close() => gameObject.SetActive(false);

    void OnEnable() => ODPassthroughBlur.Acquire(this);   // frosted room behind the glass widgets

    void OnDisable() => ODPassthroughBlur.Release(this);

    void Update() => Feed();

    /// <summary>Pushes the feed's frame to every widget when it has a new one.</summary>
    void Feed()
    {
        var feed = ODMockTelemetry.Shared;
        if (feed.Version == _version) return;
        _version = feed.Version;
        foreach (var widget in _widgets) widget.Show(feed.Frame);
    }

    void PlaceBeside()
    {
        if (_raceWindow == null) return;
        var rect = (RectTransform)transform;
        float offset = _raceWindow.rect.width * Mathf.Abs(_raceWindow.lossyScale.x) * .5f
                     + rect.rect.width * Mathf.Abs(rect.lossyScale.x) * .5f
                     + D.Px(WindowGap) * Mathf.Abs(_raceWindow.lossyScale.x);
        var centre = _raceWindow.TransformPoint(_raceWindow.rect.center);
        transform.SetPositionAndRotation(centre - _raceWindow.right * offset, _raceWindow.rotation);
        transform.position -= transform.TransformVector(rect.rect.center);
    }
}
