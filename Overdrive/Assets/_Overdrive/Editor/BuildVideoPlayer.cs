using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Builds the floating "VideoPlayerCanvas": a liquid-glass world-space window
/// with the video surface (RawImage + RenderTexture), a scrub bar, transport
/// buttons (⏮10 · play/pause · 10⏭), a back button (top-left), and the
/// draggable WindowHandle pill. Wires VideoPlayerController automatically.
/// Re-runnable.
/// </summary>
public static class BuildVideoPlayer
{
    // ── layout (canvas local units, scale 0.001 ⇒ 1000 units = 1 m) ──────────
    const float W = 1280f, H = 860f;      // window
    const float VIDEO_H = 700f;           // video surface height
    const float PAD = 24f;

    // ── palette (liquid glass, consistent with the menu) ─────────────────────
    static readonly Color Glass    = new Color(0.10f, 0.09f, 0.08f, 0.86f);
    static readonly Color GlassSoft= new Color(0.22f, 0.20f, 0.17f, 0.90f);
    static readonly Color Track    = new Color(0.35f, 0.33f, 0.29f, 0.8f);
    static readonly Color Gold     = new Color(0.85f, 0.70f, 0.20f, 1f);
    static readonly Color Txt      = new Color(0.94f, 0.92f, 0.88f, 1f);

    const string RT_PATH   = "Assets/_Overdrive/DevAssets/VideoRenderTexture.renderTexture";
    const string VIDEO_DIR = "Assets/_Overdrive/DevAssets/Video";

    [MenuItem("Overdrive/Build Video Player")]
    public static void Build()
    {
        var old = GameObject.Find("VideoPlayerCanvas");
        if (old != null) Object.DestroyImmediate(old);
        // also find inactive leftover
        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c != null && c.gameObject.name == "VideoPlayerCanvas"
                && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);

        // ── RenderTexture asset ───────────────────────────────────────────────
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
        if (rt == null)
        {
            rt = new RenderTexture(1280, 720, 0);
            AssetDatabase.CreateAsset(rt, RT_PATH);
        }

        // ── Video clip (first file in DataMockup/Video) ───────────────────────
        VideoClip clip = null;
        foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", new[] { VIDEO_DIR }))
        {
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null) break;
        }
        if (clip == null) Debug.LogWarning("[VideoPlayer] No VideoClip found in " + VIDEO_DIR);

        // ── Canvas root ───────────────────────────────────────────────────────
        var root = new GameObject("VideoPlayerCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);
        rootRT.localScale = Vector3.one * 0.001f;
        root.transform.position = new Vector3(0f, 1.1f, 1.6f);

        // glass panel background
        var bg = NewGO("GlassPanel", root.transform);
        StretchRT(bg);
        Rounded(bg, Glass, 34f);

        // ── VideoPlayer component + surface ───────────────────────────────────
        var vp = root.AddComponent<VideoPlayer>();
        vp.playOnAwake      = false;
        vp.renderMode       = VideoRenderMode.RenderTexture;
        vp.targetTexture    = rt;
        vp.audioOutputMode  = VideoAudioOutputMode.Direct;
        vp.isLooping        = false;
        vp.clip             = clip;

        var videoGO = NewGO("VideoSurface", root.transform);
        var vRT = videoGO.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0, 1); vRT.anchorMax = new Vector2(1, 1);
        vRT.pivot = new Vector2(0.5f, 1);
        vRT.anchoredPosition = new Vector2(0, -PAD);
        vRT.offsetMin = new Vector2(PAD, -PAD - VIDEO_H);
        vRT.offsetMax = new Vector2(-PAD, -PAD);
        var raw = videoGO.AddComponent<RawImage>();
        raw.texture = rt;
        raw.color = Color.white;

        // ── Back button (top-left corner over the video) ──────────────────────
        var back = NewGO("BackButton", videoGO.transform);
        var bRT = back.GetComponent<RectTransform>();
        bRT.anchorMin = bRT.anchorMax = new Vector2(0, 1); bRT.pivot = new Vector2(0, 1);
        bRT.anchoredPosition = new Vector2(16, -16); bRT.sizeDelta = new Vector2(88, 52);
        var backBg = Rounded(back, GlassSoft, 26f);
        backBg.raycastTarget = true;
        var backBtn = back.AddComponent<Button>(); backBtn.targetGraphic = backBg;
        Label(back.transform, "Label", "‹ Back", 20f, FontStyles.Bold, Txt, TextAlignmentOptions.Center, stretch: true);

        // ── Title (top-center over the video) ─────────────────────────────────
        var title = Label(videoGO.transform, "Title", "Race", 20f, FontStyles.Bold, Txt, TextAlignmentOptions.Center);
        var tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 1); tRT.anchorMax = new Vector2(0.5f, 1); tRT.pivot = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, -20); tRT.sizeDelta = new Vector2(600, 32);

        // ── Scrub bar (below the video) ───────────────────────────────────────
        float scrubY = -(PAD + VIDEO_H + 22f);
        var slider = BuildScrubBar(root.transform, scrubY);

        // ── Transport row ─────────────────────────────────────────────────────
        float rowY = scrubY - 40f;
        var rew  = TransportButton(root.transform, "Rewind10",  new Vector2(-140f, rowY), "⏴ 10s");
        var play = TransportButton(root.transform, "PlayPause", new Vector2(   0f, rowY), "❚❚", big: true);
        var fwd  = TransportButton(root.transform, "Forward10", new Vector2( 140f, rowY), "10s ⏵");

        // Time label (right side of the transport row)
        var time = Label(root.transform, "TimeLabel", "0:00 / 0:00", 17f, FontStyles.Normal,
                         new Color(0.7f, 0.68f, 0.64f, 1f), TextAlignmentOptions.MidlineRight);
        var timeRT = time.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(1, 1); timeRT.anchorMax = new Vector2(1, 1); timeRT.pivot = new Vector2(1, 0.5f);
        timeRT.anchoredPosition = new Vector2(-PAD - 8f, rowY); timeRT.sizeDelta = new Vector2(220, 30);

        // ── Controller wiring ─────────────────────────────────────────────────
        var ctrl = root.AddComponent<VideoPlayerController>();
        ctrl.videoPlayer    = vp;
        ctrl.scrubSlider    = slider;
        ctrl.playPauseLabel = play.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        ctrl.timeLabel      = time.GetComponent<TextMeshProUGUI>();
        ctrl.titleLabel     = title.GetComponent<TextMeshProUGUI>();
        var menuGO = GameObject.Find("OverdriveMenuCanvas");
        if (menuGO != null) ctrl.menuCanvas = menuGO.GetComponent<Canvas>();

        backBtn.onClick.AddListener(ctrl.BackToMenu);
        play.GetComponent<Button>().onClick.AddListener(ctrl.TogglePlayPause);
        rew.GetComponent<Button>().onClick.AddListener(() => ctrl.SkipSeconds(-10f));
        fwd.GetComponent<Button>().onClick.AddListener(() => ctrl.SkipSeconds(+10f));

        // ── Draggable pill handle ─────────────────────────────────────────────
        root.AddComponent<WindowHandle>();

        // Ray interaction so ISDK ray can click the buttons
        // (PointableCanvas is added by re-running the wizard; note in log)
        root.SetActive(false); // hidden until a thumbnail is clicked

        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[VideoPlayer] Built! NOTE: select VideoPlayerCanvas and run " +
                  "GameObject > Interaction SDK > Add Ray Interaction to Canvas " +
                  "so the ISDK ray can press its buttons.");
        Selection.activeGameObject = root;
    }

    // ── scrub bar ───────────────────────────────────────────────────────────
    static Slider BuildScrubBar(Transform parent, float y)
    {
        var go = NewGO("ScrubBar", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.offsetMin = new Vector2(PAD + 8f, y - 10f);
        rt.offsetMax = new Vector2(-PAD - 8f, y + 10f);

        var slider = go.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f; slider.maxValue = 1f;

        // track
        var track = NewGO("Background", go.transform);
        var trRT = StretchRT(track);
        trRT.offsetMin = new Vector2(0, 6); trRT.offsetMax = new Vector2(0, -6);
        Rounded(track, Track, 5f);

        // fill
        var fillArea = NewGO("FillArea", go.transform);
        var faRT = StretchRT(fillArea);
        faRT.offsetMin = new Vector2(0, 6); faRT.offsetMax = new Vector2(0, -6);
        var fill = NewGO("Fill", fillArea.transform);
        var fRT = StretchRT(fill);
        Rounded(fill, Gold, 5f);
        slider.fillRect = fRT;

        // handle
        var handleArea = NewGO("HandleSlideArea", go.transform);
        StretchRT(handleArea);
        var handle = NewGO("Handle", handleArea.transform);
        var hRT = handle.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(26, 26);
        var hImg = Rounded(handle, Color.white, 13f);
        hImg.raycastTarget = true;
        slider.handleRect = hRT;
        slider.targetGraphic = hImg;

        return slider;
    }

    // ── transport button ─────────────────────────────────────────────────────
    static GameObject TransportButton(Transform parent, string name, Vector2 pos, string txt, bool big = false)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = big ? new Vector2(84, 60) : new Vector2(110, 52);

        var bg = Rounded(go, GlassSoft, big ? 30f : 26f);
        bg.raycastTarget = true;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;

        var cb = btn.colors;
        cb.highlightedColor = new Color(0.32f, 0.29f, 0.24f, 1f);
        cb.pressedColor     = new Color(0.42f, 0.36f, 0.26f, 1f);
        btn.colors = cb;

        Label(go.transform, "Label", txt, big ? 26f : 18f, FontStyles.Bold, Txt, TextAlignmentOptions.Center, stretch: true);
        return go;
    }

    // ── primitives ───────────────────────────────────────────────────────────
    static GameObject NewGO(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static RectTransform StretchRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RoundedImage Rounded(GameObject go, Color c, float r)
    {
        var img = go.AddComponent<RoundedImage>();
        img.color = c; img.cornerRadius = r; img.cornerSegments = 12;
        img.raycastTarget = false;
        return img;
    }

    static GameObject Label(Transform p, string name, string txt, float size,
        FontStyles style, Color color, TextAlignmentOptions align, bool stretch = false)
    {
        var go = NewGO(name, p);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableWordWrapping = false;
        if (stretch) StretchRT(go);
        return go;
    }
}
