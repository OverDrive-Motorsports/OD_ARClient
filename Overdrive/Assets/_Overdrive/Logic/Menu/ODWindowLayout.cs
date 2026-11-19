/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODWindowLayout - Arranges the windows around the user on one arc
 ## (ODWindowPlacement), all at the same height and facing the headset:
 ##   • centre: the race TV while it is open, else the main window;
 ##   • right of the TV: the main window;
 ##   • left of the centre: secondary windows opened with OpenOnLeft, in the
 ##     order they were opened, side by side (the widget catalogue opens in
 ##     front of the user instead, without moving the others).
 ## When the TV appears it takes the centre in front of the headset and the
 ## main window glides to its right; when it closes, the main window glides
 ## back to the centre. Opening a secondary window keeps the centre where it
 ## stands. Windows already on screen glide (ODWindowMotion); a window that
 ## is appearing is put in place at once, under its fade-in.
 ## Recentre (Meta recenter, headset put on) puts them back in front at once.
 ## Layout only runs on these events: nothing is computed per frame.
 ##
 */

using System.Collections.Generic;
using UnityEngine;

public static class ODWindowLayout
{
    /// <summary>Secondary windows on the left, nearest the centre first.</summary>
    static readonly List<RectTransform> Left = new();
    /// <summary>The window holding the centre instead of the main one (the race TV), while it is open.</summary>
    static RectTransform _centre;
    static OverdriveMainMenu _main;
    static AppLauncher _launcher;
    static bool _quitting;

    /// <summary>Fresh state on every play session (statics survive when domain reload is off), no layout while quitting.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        Left.Clear();
        _centre = null;
        _main = null;
        _launcher = null;
        _quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    static void OnQuitting() => _quitting = true;

    /// <summary>
    /// A secondary window is opening (called just before it is shown): it takes the next place on the
    /// left; the windows already there stay or glide back into line.
    /// </summary>
    public static void OpenOnLeft(RectTransform window)
    {
        if (window == null) return;
        Left.Remove(window);
        Left.Add(window);
        Arrange(recentre: false, appearing: window);
    }

    /// <summary>The race TV appears: centre, in front of the headset; the main window glides to its right.</summary>
    public static void OpenAtCentre(RectTransform window)
    {
        if (window == null) return;
        Left.Remove(window);
        _centre = window;
        Arrange(recentre: true, appearing: window);
    }

    /// <summary>The race TV is gone: the main window glides back to the centre, in front of the headset.</summary>
    public static void CloseCentre(RectTransform window)
    {
        if (window == null || window != _centre) return;
        _centre = null;
        if (Application.isPlaying && !_quitting) Arrange(recentre: true, appearing: null);
    }

    /// <summary>
    /// The user recentred (Meta button) or put the headset on: the centre window (TV or main) goes
    /// back in front of the headset at once, the others into line around it.
    /// </summary>
    public static void Recentre()
    {
        if (Application.isPlaying && !_quitting) Arrange(recentre: true, appearing: null, snap: true);
    }

    // ── Layout ──────────────────────────────────────────────────────────────

    static void Arrange(bool recentre, RectTransform appearing, bool snap = false)
    {
        var head = Camera.main != null ? Camera.main.transform : null;
        var main = MainWindow;
        if (head == null || main == null) return;

        Left.RemoveAll(w => w == null || (w != appearing && !w.gameObject.activeInHierarchy));
        var centre = _centre != null && (_centre == appearing || _centre.gameObject.activeInHierarchy) ? _centre : main;

        var arc = recentre ? ODWindowPlacement.Arc.InFront(head) : ODWindowPlacement.Arc.Through(head, TargetCentre(centre));
        Move(centre, arc.At(0f), appearing, snap);

        if (centre != main)
            Move(main, arc.At(arc.Step(ODWindowPlacement.HalfWidth(centre), ODWindowPlacement.HalfWidth(main))), appearing, snap);

        float angle = 0f;
        var previous = centre;
        foreach (var window in Left)
        {
            angle -= arc.Step(ODWindowPlacement.HalfWidth(previous), ODWindowPlacement.HalfWidth(window));
            Move(window, arc.At(angle), appearing, snap);
            previous = window;
        }
    }

    /// <summary>Glides (or, when it is appearing, puts) <paramref name="window"/> so its centre lands on <paramref name="target"/>.</summary>
    static void Move(RectTransform window, Pose target, RectTransform appearing, bool snap)
    {
        var root = Root(window);
        var pose = ODWindowPlacement.RootPose(root, window, target);   // the rect's centre, not its pivot
        bool onScreen = !snap && window != appearing && root.gameObject.activeInHierarchy;
        ODWindowMotion.MoveTo(root, pose, onScreen, window == MainWindow ? NavigationFollower : null);
    }

    /// <summary>Centre of a window where it is going (it may be gliding).</summary>
    static Vector3 TargetCentre(RectTransform window)
    {
        var root = Root(window);
        return root.TryGetComponent(out ODWindowMotion motion)
            ? ODWindowPlacement.CentreAt(root, window, motion.Target)
            : ODWindowPlacement.Centre(window);
    }

    /// <summary>The transform that moves the window: the main window's canvas is larger than the window it shows.</summary>
    static Transform Root(RectTransform window)
    {
        if (window != MainWindow) return window;
        var canvas = window.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas.transform : window;
    }

    static RectTransform MainWindow
    {
        get
        {
            if (_main == null) _main = Object.FindAnyObjectByType<OverdriveMainMenu>();
            return _main != null ? (RectTransform)_main.transform : null;
        }
    }

    /// <summary>The floating navigation bar under the main window, unless it lives on the wrist.</summary>
    static Transform NavigationFollower
    {
        get
        {
            if (_launcher == null) _launcher = Object.FindAnyObjectByType<AppLauncher>();
            return _launcher != null ? _launcher.NavigationFollower : null;
        }
    }
}
