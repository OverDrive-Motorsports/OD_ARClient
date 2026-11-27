/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWidgetSpace - The telemetry widgets the user placed in their space
 ## from the catalogue ("Add to my space"). Each widget is a window of its
 ## own: a world canvas the size of its format (Small / Large) at 3/4 of the
 ## race window's scale, with the window bar under it to grab and move it
 ## anywhere (WindowHandle, it turns to face the user while dragged) and a
 ## round × button beside the bar to remove it.
 ## A new widget appears in front of the headset, a little below the eyes,
 ## on the spot of that row nearest the gaze (straight ahead, else right
 ## against what is there) that, seen from the head, covers neither another
 ## widget nor the open catalogue.
 ## Each widget follows its own car (WidgetTarget), the race leader until
 ## the user picks another; a widget comparing two cars follows P1 and P2.
 ## Its driver pill opens the driver picker (ODDriverPickerView): it hangs
 ## on the widget's right edge, tops aligned, in the widget's plane, and
 ## moves with it; one is open at a time and a choice closes it (for two
 ## cars, picking the followed one moves on to the compared one). Widgets
 ## are live: at each publish of the backend feed (Feed, an
 ## ODTelemetryFeed the race page points at the replay or the live session)
 ## every widget gets the frame of its car, or why there is none: loading,
 ## no data and connection lost hide its values.
 ## The race page hides the space when it is left, and shows it again on
 ## the live and replay states (SetVisible); widgets keep their place.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using static ODTelemetryStyle;
using D = ODDesign;

public sealed class ODWidgetSpace : MonoBehaviour
{
    /// <summary>Widgets are secondary windows: 3/4 of the race window's scale.</summary>
    const float SizeRatio = .75f;
    /// <summary>Where a new widget appears: metres in front of the headset, below the eyes, gap between neighbours.</summary>
    const float Distance = 1f, Drop = .18f, SlotGap = .04f;
    /// <summary>Strip under the card (design px) holding the window bar and the remove button.</summary>
    const float StripPx = 88f, RemovePx = 64f;
    /// <summary>Window bar width (m): the 1920-px windows' bar would be wider than a small widget.</summary>
    const float BarWidth = .09f;
    const float FadeSeconds = .18f;
    /// <summary>
    /// Metres the driver picker sits in front of its widget's plane: none to the eye, but the ray
    /// meets the picker first where it overlaps a neighbouring widget.
    /// </summary>
    const float PickerLift = .002f;

    /// <summary>A widget was added or removed: the catalogue updates its counts.</summary>
    public event Action Changed;

    sealed class Placed
    {
        public string id;
        public ODTelemetryWidget widget;
        public Transform window;
        public CanvasGroup group;
        public float halfWidth, fade;
        /// <summary>
        /// What the widget follows: the followed car, then the compared one for a widget comparing two
        /// (P1, P2 until the user picks); the cars they give now (-1: not resolved yet); its own frame.
        /// </summary>
        public WidgetTarget[] targets;
        public readonly int[] numbers = { -1, -1 };
        public readonly TelemetryFrame frame = new();
        public TelemetryStatus status = (TelemetryStatus)(-1);
        public string message;
    }

    readonly List<Placed> _placed = new();
    RectTransform _raceWindow;
    ODTelemetryFeed _feed;
    int _version = -1, _orderVersion = -1;
    TelemetryStatus _status;
    string _message;
    bool _blurHeld, _dirty;
    ODDriverPickerView _picker;
    Placed _pickerFor;

    public int Count => _placed.Count;

    /// <summary>Backend telemetry shown by every placed widget; the race page picks its source.</summary>
    public ODTelemetryFeed Feed => _feed;

    public int CountOf(string id) => _placed.FindAll(p => p.id == id).Count;


    /// <summary>The (empty) space and its feed; widgets are sized from <paramref name="raceWindow"/>'s scale.</summary>
    public static ODWidgetSpace Create(RectTransform raceWindow)
    {
        var go = new GameObject("WidgetSpace");
        var space = go.AddComponent<ODWidgetSpace>();
        space._raceWindow = raceWindow;
        space._feed = go.AddComponent<ODTelemetryFeed>();
        space._status = space._feed.Status;
        space._message = space._feed.Message;
        return space;
    }

    /// <summary>
    /// Puts a live widget of <paramref name="type"/> (an ODTelemetryWidget) in the space, in front of the
    /// user, where it hides neither another widget nor <paramref name="avoid"/> (the catalogue).
    /// </summary>
    public ODTelemetryWidget Add(string id, Type type, WidgetFormat format, RectTransform avoid = null)
    {
        if (type == null) return null;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Vector2 size = SizeOf(format);

        var canvas = ODPointableCanvas.Create("Widget " + id + " " + format, new Vector2(D.Px(size.x), D.Px(size.y + StripPx)));
        var window = canvas.transform;
        window.SetParent(transform, false);
        float scale = (_raceWindow != null ? Mathf.Abs(_raceWindow.lossyScale.x) : .001f) * SizeRatio;
        window.localScale = Vector3.one * scale;   // before WindowHandle.Start sizes its bar from it

        var widget = ODTelemetryWidget.Create(type, window, 0f, 0f, format);
        var placed = new Placed
        {
            id = id,
            widget = widget,
            window = window,
            group = canvas.gameObject.AddComponent<CanvasGroup>(),
            halfWidth = D.Px(size.x) * scale * .5f,
        };
        placed.group.alpha = 0f;

        var handle = canvas.gameObject.AddComponent<WindowHandle>();
        handle.barWidth = BarWidth;
        handle.followTarget = (RectTransform)widget.transform;   // the bar sits under the card, not the strip

        var remove = D.IconButton(window, "Remove", size.x - RemovePx, size.y + (StripPx - RemovePx) * .5f, RemovePx,
            ODNavigationIcon.Symbol.Close, iconPx: 24f);
        remove.onClick.AddListener(() => Remove(placed));

        widget.EnablePicker();
        widget.DriverPillClicked += () => TogglePicker(placed);
        placed.targets = widget.Compares
            ? new[] { WidgetTarget.Position(1), WidgetTarget.Position(2) }
            : new[] { WidgetTarget.Leader };

        Place(placed, Mathf.Abs(D.Px(size.y + StripPx) * scale) * .5f, avoid);
        _placed.Add(placed);
        Refresh(placed);   // right away, not at the next publish: the current values, or why there are none
        UpdateBlur();
        Changed?.Invoke();
        return widget;
    }

    /// <summary>What <paramref name="widget"/> follows in <paramref name="slot"/> (0 followed, 1 compared).</summary>
    public WidgetTarget TargetOf(ODTelemetryWidget widget, int slot = 0)
    {
        var targets = _placed.Find(p => p.widget == widget)?.targets;
        return targets != null && slot < targets.Length ? targets[slot] : WidgetTarget.Position(slot + 1);
    }

    /// <summary>Makes <paramref name="widget"/> follow <paramref name="target"/> in <paramref name="slot"/> (0 followed, 1 compared).</summary>
    public void Assign(ODTelemetryWidget widget, WidgetTarget target, int slot = 0)
    {
        var placed = _placed.Find(p => p.widget == widget);
        if (placed != null) Assign(placed, slot, target);
    }

    /// <summary>One slot; a car already in the other slot swaps places with it rather than being shown twice.</summary>
    void Assign(Placed placed, int slot, WidgetTarget target)
    {
        var targets = placed.targets;
        if (slot < 0 || slot >= targets.Length) return;
        int other = 1 - slot;
        int car = _feed.Resolve(target);
        if (targets.Length > 1 && car > 0 && _feed.Resolve(targets[other]) == car) targets[other] = targets[slot];
        targets[slot] = target;
        _dirty = true;
    }

    // ── Driver picker ───────────────────────────────────────────────────────

    void TogglePicker(Placed placed)
    {
        if (_pickerFor == placed) ClosePicker();
        else OpenPicker(placed);
    }

    void OpenPicker(Placed placed)
    {
        ClosePicker();
        _pickerFor = placed;
        placed.widget.SetPickerOpen(true);
        _picker = ODDriverPickerView.Create(transform, placed.widget.Name, placed.targets.Length, ODTelemetryWidget.PillMiddle);
        _picker.Picked += (slot, target) =>
        {
            if (_pickerFor == null) return;
            Assign(_pickerFor, slot, target);
            // Two cars: the followed one picked, the compared one is next; otherwise done.
            if (slot == 0 && _pickerFor.targets.Length > 1) _picker.SetActiveSlot(1);
            else ClosePicker();
        };
        _picker.LeadersPicked += () =>
        {
            if (_pickerFor == null) return;
            for (int i = 0; i < _pickerFor.targets.Length; i++) _pickerFor.targets[i] = WidgetTarget.Position(i + 1);
            _dirty = true;
            ClosePicker();
        };
        _picker.Closed += ClosePicker;
        _orderVersion = -1;
        FollowWidget();
        UpdatePicker();
    }

    void ClosePicker()
    {
        if (_pickerFor != null && _pickerFor.widget != null) _pickerFor.widget.SetPickerOpen(false);
        _pickerFor = null;
        if (_picker != null) Destroy(_picker.gameObject);
        _picker = null;
    }

    /// <summary>The picker hangs on the widget's right edge, tops aligned, in its plane, and moves with it.</summary>
    void FollowWidget()
    {
        var window = (RectTransform)_pickerFor.window;
        var picker = (RectTransform)_picker.transform;
        Vector2 widget = window.sizeDelta, size = picker.sizeDelta;
        // Window coordinates from its centre: right of its right edge, top on its top (the card's top).
        var local = new Vector3((widget.x + size.x) * .5f, (widget.y - size.y) * .5f, 0f);
        picker.SetPositionAndRotation(window.TransformPoint(local) - window.forward * PickerLift, window.rotation);
        picker.localScale = window.localScale;
    }

    void UpdatePicker()
    {
        var placed = _pickerFor;
        if (_orderVersion != _feed.OrderVersion)
        {
            _orderVersion = _feed.OrderVersion;
            _picker.SetOrder(_feed.Order);
        }
        var followed = new DriverChoice[placed.targets.Length];
        for (int i = 0; i < followed.Length; i++) followed[i] = _feed.Choice(placed.numbers[i]);
        _picker.SetSelection(placed.targets, followed);
    }

    /// <summary>Hides (race page left) or shows again (live / replay) every placed widget, where it was.</summary>
    public void SetVisible(bool visible)
    {
        if (!visible) ClosePicker();
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }

    /// <summary>Takes <paramref name="widget"/> out of the space (its × button does it too).</summary>
    public void Remove(ODTelemetryWidget widget)
    {
        var placed = _placed.Find(p => p.widget == widget);
        if (placed != null) Remove(placed);
    }

    void Remove(Placed placed)
    {
        if (_pickerFor == placed) ClosePicker();
        if (!_placed.Remove(placed)) return;
        if (placed.window != null) Destroy(placed.window.gameObject);
        UpdateBlur();
        Changed?.Invoke();
    }

    void OnEnable() => UpdateBlur();

    void OnDisable() => UpdateBlur();

    /// <summary>Frosted room behind the glass widgets while any is on screen.</summary>
    void UpdateBlur()
    {
        bool want = isActiveAndEnabled && _placed.Count > 0;
        if (want == _blurHeld) return;
        _blurHeld = want;
        if (want) ODPassthroughBlur.Acquire(this);
        else ODPassthroughBlur.Release(this);
    }

    void Update()
    {
        foreach (var placed in _placed)
        {
            if (placed.fade >= 1f) continue;
            placed.fade = Mathf.Min(1f, placed.fade + ODTime.Delta / FadeSeconds);
            placed.group.alpha = ODRuntimeUI.EaseOutCubic(placed.fade);
        }

        // Every widget again at each publish, when the source's status changes, or when a target changed.
        if (_dirty || _feed.Version != _version || _feed.Status != _status || _feed.Message != _message)
        {
            _dirty = false;
            _version = _feed.Version;
            _status = _feed.Status;
            _message = _feed.Message;
            foreach (var placed in _placed) Refresh(placed);
        }

        if (_pickerFor != null)
        {
            FollowWidget();
            UpdatePicker();
        }
    }

    /// <summary>The widget's car (and compared car) now, its frame, or why there is none.</summary>
    void Refresh(Placed placed)
    {
        int number = _feed.Resolve(placed.targets[0]), rival = 0;
        if (placed.targets.Length > 1)
        {
            rival = _feed.Resolve(placed.targets[1]);
            if (rival == number) rival = _feed.Other(number);   // never the same car twice
        }
        var status = _feed.Fill(number, rival, placed.frame, out string message);
        if (number != placed.numbers[0] || rival != placed.numbers[1])
        {
            placed.numbers[0] = number;
            placed.numbers[1] = rival;
            placed.widget.ShowTarget(_feed.Choice(number), _feed.Choice(rival));
            placed.widget.Restart();   // another car: shown at once, nothing eased from the previous one
        }
        if (status != placed.status || message != placed.message)
        {
            placed.status = status;
            placed.message = message;
            placed.widget.ShowStatus(status, message, null);
        }
        if (status == TelemetryStatus.Ready) placed.widget.Show(placed.frame);
    }

    // ── Placement ───────────────────────────────────────────────────────────

    /// <summary>Head-relative box seen from the head: yaw and pitch ranges in degrees.</summary>
    struct View
    {
        public float yawMin, yawMax, pitchMin, pitchMax;

        public bool Overlaps(View other) =>
            yawMin < other.yawMax && other.yawMin < yawMax && pitchMin < other.pitchMax && other.pitchMin < pitchMax;
    }

    /// <summary>
    /// In front of the headset, below the eyes, facing it: the centre of the row, else the nearest spot
    /// beside it (right first) that covers neither a widget nor <paramref name="avoid"/>, seen from the head.
    /// </summary>
    void Place(Placed placed, float halfHeight, RectTransform avoid)
    {
        var cam = Camera.main;
        Vector3 head = cam != null ? cam.transform.position : Vector3.up * 1.6f;
        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.forward;
        if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
        forward.Normalize();
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        var taken = new List<View>();
        foreach (var other in _placed)
            if (other.window != null && other.window.gameObject.activeInHierarchy) taken.Add(ViewOf(other.window as RectTransform, head, yaw));
        if (avoid != null && avoid.gameObject.activeInHierarchy) taken.Add(ViewOf(avoid, head, yaw));

        // Candidate spots on the row: straight ahead, and right against each side of what is
        // already there (the gap between), nearest the gaze first, right before left.
        float half = Mathf.Atan2(placed.halfWidth, Distance) * Mathf.Rad2Deg, gap = SlotGap / Distance * Mathf.Rad2Deg;
        var angles = new List<float> { 0f };
        foreach (var t in taken)
        {
            angles.Add(t.yawMax + gap + half);
            angles.Add(t.yawMin - gap - half);
        }
        angles.Sort((x, y) => Mathf.Abs(x) != Mathf.Abs(y) ? Mathf.Abs(x).CompareTo(Mathf.Abs(y)) : y.CompareTo(x));

        var pose = SpotAt(head, yaw, 0f);
        foreach (float angle in angles)
        {
            var spot = SpotAt(head, yaw, angle);
            var view = ViewOf(spot, placed.halfWidth, halfHeight, head, yaw);
            if (taken.TrueForAll(t => !t.Overlaps(view))) { pose = spot; break; }
        }
        placed.window.SetPositionAndRotation(pose.position, pose.rotation);
    }

    /// <summary>Spot of the row <paramref name="angle"/> degrees right of <paramref name="yaw"/>, facing the head.</summary>
    static Pose SpotAt(Vector3 head, float yaw, float angle)
    {
        var rotation = Quaternion.Euler(0f, yaw + angle, 0f);
        return new Pose(head + rotation * Vector3.forward * Distance + Vector3.down * Drop, rotation);
    }

    /// <summary>What a window of half size (w, h) at <paramref name="pose"/> covers, seen from the head.</summary>
    static View ViewOf(Pose pose, float halfW, float halfH, Vector3 head, float yaw)
    {
        Vector3 right = pose.rotation * Vector3.right * halfW, up = pose.rotation * Vector3.up * halfH;
        return ViewOf(new[] { pose.position - right - up, pose.position - right + up, pose.position + right + up, pose.position + right - up }, head, yaw);
    }

    static View ViewOf(RectTransform rect, Vector3 head, float yaw)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return ViewOf(corners, head, yaw);
    }

    static View ViewOf(Vector3[] corners, Vector3 head, float yaw)
    {
        var view = new View { yawMin = float.MaxValue, yawMax = float.MinValue, pitchMin = float.MaxValue, pitchMax = float.MinValue };
        foreach (var corner in corners)
        {
            Vector3 d = corner - head;
            float flat = new Vector2(d.x, d.z).magnitude;
            float a = Mathf.DeltaAngle(yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
            float p = Mathf.Atan2(d.y, flat) * Mathf.Rad2Deg;
            view.yawMin = Mathf.Min(view.yawMin, a);
            view.yawMax = Mathf.Max(view.yawMax, a);
            view.pitchMin = Mathf.Min(view.pitchMin, p);
            view.pitchMax = Mathf.Max(view.pitchMax, p);
        }
        return view;
    }
}
