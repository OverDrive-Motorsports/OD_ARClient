using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ODVideoUI;

/// <summary>Overdrive's course TV, multi-selection dialog and dynamic grouped/surround views.</summary>
public sealed class ODVideoPlayerView : MonoBehaviour
{
    public enum Layout { Around, Mosaic }
    public GameObject pilotPrefab;
    public GameObject pickerPrefab;
    public RawImage mainVideo;
    public RectTransform mosaicRoot;
    public TextMeshProUGUI badgeLabel;
    public TextMeshProUGUI feedLabel;
    public ODVideoIcon playIcon;
    public ODVideoIcon volumeIcon;
    public Button addViewButton;
    public Button moreButton;
    public Button expandButton;
    public Button muteButton;
    public Button rewindButton;
    public Button forwardButton;
    public GameObject dimmer;
    public GameObject optionsPanel;
    public Button regroupButton;
    public Button closeViewsButton;
    public Layout CurrentLayout { get; private set; }
    public int PrimaryFeed { get; private set; } = -1;
    public int ViewCount => _feeds.Count;
    public bool PickerVisible => _picker != null && _picker.activeSelf;
    public Texture VideoTexture => mainVideo != null ? mainVideo.texture : null;
    public IReadOnlyList<ODPilotVideoView> Satellites => _satellites;
    public ODPilotVideoView GroupedView => _groupedView;
    [NonSerialized] public VideoPlayerController controller;

    // Demo catalogue only when the course has no participants yet; numbers are never array indices.
    private static readonly string[] Names = { "Max Verstappen", "Charles Leclerc", "Lando Norris", "George Russell", "Oscar Piastri", "Lewis Hamilton", "Carlos Sainz", "Fernando Alonso", "Pierre Gasly", "Esteban Ocon", "Yuki Tsunoda", "Alexander Albon", "Nico Hülkenberg", "Lance Stroll", "Liam Lawson", "Oliver Bearman", "Andrea Kimi Antonelli", "Isack Hadjar", "Gabriel Bortoleto", "Franco Colapinto" };
    private static readonly string[] Teams = { "Red Bull Racing", "Ferrari", "McLaren", "Mercedes", "McLaren", "Ferrari", "Williams", "Aston Martin", "Alpine", "Haas", "Red Bull Racing", "Williams", "Sauber", "Aston Martin", "Racing Bulls", "Haas", "Mercedes", "Racing Bulls", "Sauber", "Alpine" };
    private static readonly int[] Numbers = { 1, 16, 4, 63, 81, 44, 55, 14, 10, 31, 22, 23, 27, 18, 30, 87, 12, 6, 5, 43 };
    private readonly List<DriverSummaryDto> _drivers = new List<DriverSummaryDto>();
    public int DriverCount => _drivers.Count > 0 ? _drivers.Count : Names.Length;
    public int DriverNumber(int feed) => feed < 0 || feed >= DriverCount ? 0 : _drivers.Count > 0 ? _drivers[feed].driverNumber : Numbers[feed];
    private string DriverName(int feed) => _drivers.Count > 0 ?
        (string.IsNullOrWhiteSpace(_drivers[feed].fullName) ? (_drivers[feed].firstName + " " + _drivers[feed].lastName).Trim() : _drivers[feed].fullName) : Names[feed];
    private string TeamName(int feed) => _drivers.Count > 0 ? _drivers[feed].teamName : Teams[feed];
    private readonly List<int> _feeds = new List<int>();
    private readonly List<ODPilotVideoView> _satellites = new List<ODPilotVideoView>();
    private readonly HashSet<int> _pending = new HashSet<int>();
    private readonly List<Button> _cameraButtons = new List<Button>();
    private GameObject _picker;
    private RectTransform _cameraContent;
    private Button _confirm, _around, _mosaic;
    private ODPilotVideoView _groupedView;
    private TextMeshProUGUI _pickerSubtitle;
    private Layout _pendingLayout;
    private bool _bound, _expanded;
    private float _audibleVolume = .7f;

    public string FeedName(int feed) => feed < 0 ? "Diffusion principale" : "Onboard · " + DriverName(Mathf.Clamp(feed, 0, DriverCount - 1));
    public Texture TextureForFeed(int feed) => controller != null ? controller.TextureForFeed(feed) : null;
    public string StatusForFeed(int feed) => controller != null ? controller.StatusForFeed(feed) : "Vidéo indisponible";
    public void SyncSources() { if (controller != null) controller.SyncViews(_feeds, PrimaryFeed); }

    /// <summary>Stable session participants, not the current ranking order. A selected index never changes driver mid-playback.</summary>
    public void ResetDriverCatalogue() => _drivers.Clear();
    public void SetDrivers(IEnumerable<DriverSummaryDto> entries)
    {
        if (entries == null || _feeds.Count > 0 || PrimaryFeed >= 0) return;
        var drivers = entries.Where(d => d != null && d.driverNumber > 0).GroupBy(d => d.driverNumber)
            .Select(g => g.First()).OrderBy(d => d.driverNumber).ToList();
        if (drivers.Count == 0) return;
        _drivers.Clear(); _drivers.AddRange(drivers);
        if (PickerVisible) { BuildCameraCards(); UpdatePicker(); }
    }

    /// <summary>Bakes the principal view into the saved prefab. Also repairs a newly added empty component.</summary>
    public void BuildMain(Texture texture)
    {
        Clear(transform);
        ((RectTransform)transform).sizeDelta = new Vector2(1200, 675);
        mainVideo = Video(transform, "Video", 0, 0, 1200, 675, texture, 36);
        mosaicRoot = Rect(transform, "Mosaic", 18, 100, 1164, 475);
        Fill(transform, "Status", 30, 30, 100, 40, new Color(.6f, .12f, .10f, .9f), 20);
        badgeLabel = Text(transform, "StatusText", "DIRECT", 39, 30, 86, 40, 16, true);
        Text(transform, "Title", "Grand Prix", 148, 30, 600, 40, 25, true);
        feedLabel = Text(transform, "Feed", "Diffusion principale", 148, 54, 600, 24, 16, false, ODDesign.Secondary);
        addViewButton = Button(transform, "AddView", "Ajouter une vue", 946, 26, 180, 48);
        moreButton = IconButton(transform, "More", ODVideoIcon.Kind.More, 1136, 26, 48);
        optionsPanel = Fill(transform, "ViewOptions", 868, 86, 316, 140, Glass, 24).gameObject;
        regroupButton = Button(optionsPanel.transform, "Regroup", "Rattacher les vues", 12, 12, 292, 52);
        closeViewsButton = Button(optionsPanel.transform, "CloseAll", "Fermer les vues", 12, 76, 292, 52);
        optionsPanel.SetActive(false);
        var transport = Fill(transform, "TransportGlass", 18, 585, 1164, 72, Glass, 36);
        rewindButton = IconButton(transport.transform, "BackTen", ODVideoIcon.Kind.BackTen, 12, 12, 48, bare: true);
        Text(rewindButton.transform, "Seconds", "10", 14, 14, 20, 20, 10, true).alignment = TextAlignmentOptions.Center;
        var play = IconButton(transform, "PlayPause", ODVideoIcon.Kind.Pause, 84, 597, 48, bare: true);
        playIcon = play.GetComponentInChildren<ODVideoIcon>();
        // Compatibility label is not the visible control: the pause graphic has no circle or font glyph.
        Text(play.transform, "Label", "Pause", 0, 0, 48, 48).gameObject.SetActive(false);
        forwardButton = IconButton(transport.transform, "ForwardTen", ODVideoIcon.Kind.ForwardTen, 126, 12, 48, bare: true);
        Text(forwardButton.transform, "Seconds", "10", 14, 14, 20, 20, 10, true).alignment = TextAlignmentOptions.Center;
        Text(transform, "Time", "0:00:00", 210, 607, 92, 28, 18, true);
        Slider(transform, "Progress", 320, 601, 404, 40, 0);
        muteButton = IconButton(transform, "Mute", ODVideoIcon.Kind.Volume, 837, 597, 48, bare: true);
        volumeIcon = muteButton.GetComponentInChildren<ODVideoIcon>();
        Slider(transform, "Volume", 898, 601, 112, 40, .7f);
        expandButton = IconButton(transform, "Expand", ODVideoIcon.Kind.Expand, 1120, 597, 48, bare: true);
        Text(transform, "PlaybackStatus", "Vidéo indisponible", 30, 550, 800, 26, 15, false, ODDesign.Secondary);
        var empty = Text(transform, "VideoUnavailable", "Vidéo indisponible", 160, 260, 880, 120, 30, true);
        empty.alignment = TextAlignmentOptions.Center; empty.textWrappingMode = TextWrappingModes.Normal;
        Text(transform, "VolumeValue", "70%", 1018, 607, 48, 28, 15).gameObject.SetActive(false);
        var overlay = Fill(transform, "PickerDimmer", 0, 0, 1200, 675, new Color(0, 0, 0, .62f), 36);
        overlay.raycastTarget = true; dimmer = overlay.gameObject; dimmer.SetActive(false);
        var handle = GetComponent<WindowHandle>();
        if (handle != null) { handle.customPillPosition = false; handle.followTarget = (RectTransform)transform; }
        PolishControls();
    }

    /// <summary>Targeted, repeatable polish of the saved TV. Keeps the decoder, textures, bindings and window handle intact.</summary>
    public void PolishControls()
    {
        var progress = transform.Find("Progress")?.GetComponent<Slider>();
        var volume = transform.Find("Volume")?.GetComponent<Slider>();
        if (progress == null || volume == null || muteButton == null || optionsPanel == null)
            throw new InvalidOperationException("Build or connect the TV controls before applying polish.");
        foreach (string path in new[] { "HeaderGlass", "TransportGlass/BackendSource", "TransportGlass/DemoSource", "TransportGlass/CaptionsUnavailable", "TransportGlass/LanguageUnavailable" })
        {
            var old = transform.Find(path); if (old != null) Remove(old.gameObject);
        }
        if (feedLabel != null) { feedLabel.text = ""; feedLabel.gameObject.SetActive(false); }
        var title = transform.Find("Title") as RectTransform;
        title.anchoredPosition = new Vector2(148, -30); title.sizeDelta = new Vector2(600, 40);
        var status = transform.Find("Status").GetComponent<RoundedImage>();
        status.color = new Color(.46f, .16f, .14f, .82f);
        if (transform.Find("StatusDot") == null)
            Fill(transform, "StatusDot", 41, 47, 6, 6, new Color(1f, .33f, .25f), 3);
        badgeLabel.rectTransform.anchoredPosition = new Vector2(55, -30);
        badgeLabel.rectTransform.sizeDelta = new Vector2(68, 40);

        var transport = transform.Find("TransportGlass").GetComponent<RoundedImage>();
        var rim = transport.GetComponent<Outline>();
        if (rim == null) rim = transport.gameObject.AddComponent<Outline>();
        rim.effectColor = new Color(1, 1, 1, .12f); rim.effectDistance = new Vector2(1, -1); rim.useGraphicAlpha = true;
        ((RectTransform)optionsPanel.transform).sizeDelta = new Vector2(316, 268);
        if (optionsPanel.transform.Find("CaptionsUnavailable") == null)
            Button(optionsPanel.transform, "CaptionsUnavailable", "Sous-titres · Bientôt", 12, 140, 292, 52).interactable = false;
        if (optionsPanel.transform.Find("LanguageUnavailable") == null)
            Button(optionsPanel.transform, "LanguageUnavailable", "Langue · Bientôt", 12, 204, 292, 52).interactable = false;

        var area = progress.transform.Find("HandleArea") as RectTransform;
        area.anchoredPosition = new Vector2(0, -8); area.sizeDelta = new Vector2(area.rect.width, 24);
        var knob = progress.handleRect;
        knob.sizeDelta = new Vector2(24, 0);
        var red = new Color(.87f, .28f, .23f);
        var knobImage = knob.GetComponent<RoundedImage>(); knobImage.color = red; knobImage.cornerRadius = 12;
        if (knob.Find("Core") == null) Fill(knob, "Core", 4, 4, 16, 16, Color.white, 8);
        var halo = knob.Find("ScrubGlow") as RectTransform;
        if (halo == null)
        {
            halo = Rect(knob, "ScrubGlow", 0, 0, 128, 48);
            halo.anchorMin = halo.anchorMax = halo.pivot = new Vector2(.5f, .5f);
            halo.anchoredPosition = Vector2.zero; halo.SetAsFirstSibling();
            var graphic = halo.gameObject.AddComponent<ODVideoProgressGlow>();
            graphic.slider = progress; graphic.color = red; graphic.raycastTarget = false;
        }
        var divider = transform.Find("TransportDivider") as RectTransform;
        if (divider == null) divider = Fill(transform, "TransportDivider", 1032, 606, 1.2f, 30, new Color(1, 1, 1, .22f), .6f).rectTransform;
        var zone = transform.Find("VolumeHoverZone") as RectTransform;
        if (zone == null)
        {
            var hit = Fill(transform, "VolumeHoverZone", 1040, 589, 72, 64, Color.clear, 0);
            hit.raycastTarget = true; zone = hit.rectTransform;
        }
        zone.SetSiblingIndex(muteButton.transform.GetSiblingIndex());
        var reveal = zone.GetComponent<ODVideoVolumeReveal>();
        if (reveal == null) reveal = zone.gameObject.AddComponent<ODVideoVolumeReveal>();
        reveal.speaker = (RectTransform)muteButton.transform; reveal.volume = volume; reveal.timeline = progress; reveal.separator = divider;
        // Native Unity objects can be "fake null" in the Editor: do not use ?? for CanvasGroup.
        var sliderGroup = volume.GetComponent<CanvasGroup>();
        if (sliderGroup == null) sliderGroup = volume.gameObject.AddComponent<CanvasGroup>();
        reveal.sliderGroup = sliderGroup;
        foreach (var target in new[] { muteButton.gameObject, volume.gameObject })
        {
            var region = target.GetComponent<ODVideoVolumeReveal>();
            if (region == null) region = target.AddComponent<ODVideoVolumeReveal>();
            region.owner = reveal;
        }
        reveal.ResetPresentation();
        // Newly added decorations must not sit above the modal veil or its interaction blocker.
        if (dimmer != null) dimmer.transform.SetAsLastSibling();
    }

    public void Bind(VideoPlayerController value)
    {
        controller = value;
        if (mainVideo == null) BuildMain(value.videoPlayer != null ? value.videoPlayer.targetTexture : null);
        if (transform.Find("VolumeHoverZone") == null) PolishControls();
        value.titleLabel = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        value.playPauseButton = transform.Find("PlayPause").GetComponent<Button>();
        value.playPauseLabel = transform.Find("PlayPause/Label").GetComponent<TextMeshProUGUI>();
        value.playbackStatus = transform.Find("PlaybackStatus").GetComponent<TextMeshProUGUI>();
        value.volumeSlider = transform.Find("Volume").GetComponent<Slider>();
        value.volumeLabel = transform.Find("VolumeValue").GetComponent<TextMeshProUGUI>();
        value.scrubSlider = transform.Find("Progress").GetComponent<Slider>();
        value.timeLabel = transform.Find("Time").GetComponent<TextMeshProUGUI>();
        if (_bound) return;
        _bound = true;
        addViewButton.onClick.AddListener(OpenPicker);
        moreButton.onClick.AddListener(() => optionsPanel.SetActive(!optionsPanel.activeSelf));
        regroupButton.onClick.AddListener(() => { ReattachViews(); optionsPanel.SetActive(false); });
        closeViewsButton.onClick.AddListener(ResetViews);
        rewindButton.onClick.AddListener(() => controller.SkipSeconds(-10));
        forwardButton.onClick.AddListener(() => controller.SkipSeconds(10));
        muteButton.onClick.AddListener(ToggleMute);
        expandButton.onClick.AddListener(ToggleExpand);
        Refresh();
    }

    public void Refresh()
    {
        if (controller == null) return;
        if (addViewButton != null) addViewButton.interactable = !controller.CatalogueLoading;
        if (playIcon != null) playIcon.Set(controller.WantsPlayback ? ODVideoIcon.Kind.Pause : ODVideoIcon.Kind.Play);
        if (volumeIcon != null) volumeIcon.Set(controller.volume <= 0 ? ODVideoIcon.Kind.Mute : ODVideoIcon.Kind.Volume);
        bool replay = controller.course != null && controller.course.currentMode == ODSportPage.Mode.Replay;
        if (badgeLabel != null) badgeLabel.text = replay ? "REPLAY" : "DIRECT";
        var status = transform.Find("Status")?.GetComponent<RoundedImage>();
        if (status != null) status.color = replay ? new Color(.15f, .29f, .48f, .82f) : new Color(.46f, .16f, .14f, .82f);
        var dot = transform.Find("StatusDot")?.GetComponent<RoundedImage>();
        if (dot != null) dot.color = replay ? new Color(.5f, .72f, 1f) : new Color(1f, .33f, .25f);
        if (feedLabel != null)
        {
            feedLabel.text = PrimaryFeed >= 0 ? FeedName(PrimaryFeed) : "";
            feedLabel.gameObject.SetActive(PrimaryFeed >= 0);
        }
        var title = transform.Find("Title") as RectTransform;
        if (title != null) title.anchoredPosition = new Vector2(148, PrimaryFeed >= 0 ? -23 : -30);
        if (Application.isPlaying) ODVideoUI.BindVideoTexture(mainVideo, TextureForFeed(PrimaryFeed));
        var empty = transform.Find("VideoUnavailable")?.GetComponent<TextMeshProUGUI>();
        if (empty != null)
        {
            empty.text = StatusForFeed(PrimaryFeed);
            empty.gameObject.SetActive(!Application.isPlaying || TextureForFeed(PrimaryFeed) == null);
        }
        foreach (var satellite in _satellites) if (satellite != null) satellite.RefreshMedia();
        if (_groupedView != null) _groupedView.RefreshMedia();
    }
    private void ToggleMute()
    { if (controller.volume > 0) { _audibleVolume = controller.volume; controller.SetVolume(0); } else controller.SetVolume(_audibleVolume); Refresh(); }
    public void ToggleExpand()
    { _expanded = !_expanded; transform.localScale *= _expanded ? 1.25f : .8f; }

    public void OpenPicker()
    {
        if (pickerPrefab == null) return;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        _pending.Clear(); foreach (int feed in _feeds) _pending.Add(feed);
        _pendingLayout = CurrentLayout;
        if (_picker == null)
        {
            _picker = Instantiate(pickerPrefab); _picker.name = "OverdriveViewPicker";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_picker, gameObject.scene);
            BuildPicker();
        }
        _picker.SetActive(true); dimmer.SetActive(true);
        _picker.GetComponent<Canvas>().worldCamera = Camera.main;
        _picker.transform.localScale = transform.lossyScale;
        _picker.transform.SetPositionAndRotation(transform.TransformPoint(new Vector3(40, -26, -45)), transform.rotation);
        BuildCameraCards(); UpdatePicker();
    }
    public void ClosePicker()
    { if (_picker != null) _picker.SetActive(false); if (dimmer != null) dimmer.SetActive(false); }

    private void BuildPicker()
    {
        Clear(_picker.transform);
        // A dark frosted veil keeps the dialog readable over any broadcast, even before passthrough blur is ready.
        Fill(_picker.transform, "ModalVeil", 0, 0, 1120, 660, new Color(.13f, .14f, .16f, .96f), 36);
        Text(_picker.transform, "Heading", "Ajouter une vue", 330, 22, 460, 42, 32, true).alignment = TextAlignmentOptions.Center;
        _pickerSubtitle = Text(_picker.transform, "Subtitle", "", 220, 69, 680, 28, 17, false, ODDesign.Secondary);
        _pickerSubtitle.alignment = TextAlignmentOptions.Center;
        Button(_picker.transform, "Cancel", "Annuler", 24, 26, 150, 48, ClosePicker);
        _confirm = Button(_picker.transform, "Confirm", "Ajouter 0 vue", 886, 26, 210, 48, ConfirmPicker, true);
        Text(_picker.transform, "CameraHeading", "CAMÉRAS EMBARQUÉES", 24, 117, 480, 32, 18, true, ODDesign.Secondary);
        Text(_picker.transform, "CameraCount", "", 792, 109, 304, 44, 17, false, ODDesign.Secondary).alignment = TextAlignmentOptions.MidlineRight;
        var scroll = Rect(_picker.transform, "Cameras", 24, 169, 1072, 248).gameObject.AddComponent<ScrollRect>();
        var viewport = Rect(scroll.transform, "Viewport", 0, 0, 1032, 248);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        _cameraContent = Rect(viewport, "Content", 0, 0, 1032, 248);
        scroll.viewport = viewport; scroll.content = _cameraContent; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var sb = Rect(scroll.transform, "Scrollbar", 1044, 0, 28, 248).gameObject.AddComponent<Scrollbar>();
        var track = sb.gameObject.AddComponent<Image>(); track.color = Color.clear;
        Fill(sb.transform, "Track", 8, 0, 12, 248, ODDesign.Plateau, 6);
        var area = Rect(sb.transform, "SlidingArea", 8, 0, 12, 248);
        var thumb = Fill(area, "Handle", 0, 0, 0, 0, new Color(1, 1, 1, .60f), 6);
        sb.handleRect = thumb.rectTransform; sb.targetGraphic = thumb; sb.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = sb;
        scroll.scrollSensitivity = 45;
        Text(_picker.transform, "SpecialHeading", "VUES SPÉCIALES", 24, 435, 500, 28, 18, true, ODDesign.Secondary);
        var map = Button(_picker.transform, "MapPlaceholder", "Carte en direct", 24, 474, 524, 70);
        Icon(map.transform, "Icon", ODVideoIcon.Kind.Map, 20, 20, 30);
        map.GetComponentInChildren<TextMeshProUGUI>().rectTransform.anchoredPosition += new Vector2(20, 9);
        Text(map.transform, "ComingSoon", "Bientôt disponible", 72, 40, 420, 25, 15, false, ODDesign.Secondary);
        var radio = Button(_picker.transform, "RadioPlaceholder", "Radio des équipes", 572, 474, 524, 70);
        Icon(radio.transform, "Icon", ODVideoIcon.Kind.Radio, 20, 20, 30);
        radio.GetComponentInChildren<TextMeshProUGUI>().rectTransform.anchoredPosition += new Vector2(20, 9);
        Text(radio.transform, "ComingSoon", "Bientôt disponible", 72, 40, 420, 25, 15, false, ODDesign.Secondary);
        Fill(_picker.transform, "Divider", 24, 564, 1072, 1, ODDesign.Divider, 0);
        Text(_picker.transform, "LayoutHeading", "Disposition", 24, 584, 150, 48, 20, true);
        _around = Button(_picker.transform, "Around", "Autour de moi", 186, 584, 204, 48, () => { _pendingLayout = Layout.Around; UpdatePicker(); });
        _mosaic = Button(_picker.transform, "Mosaic", "Mosaïque", 402, 584, 168, 48, () => { _pendingLayout = Layout.Mosaic; UpdatePicker(); });
        Text(_picker.transform, "LayoutHint", "", 592, 580, 504, 58, 16, false, ODDesign.Secondary).textWrappingMode = TextWrappingModes.Normal;
    }

    private void BuildCameraCards()
    {
        Clear(_cameraContent); _cameraButtons.Clear();
        int count = DriverCount;
        _cameraContent.sizeDelta = new Vector2(1032, Mathf.Ceil(count / 4f) * 248);
        _cameraContent.anchoredPosition = Vector2.zero;
        var scroll = _cameraContent.GetComponentInParent<ScrollRect>();
        if (scroll != null && scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(count > 4);
        for (int i = 0; i < count; i++)
        {
            int feed = i; float x = i % 4 * 260, y = i / 4 * 248;
            var card = Button(_cameraContent, "Camera" + i, null, x, y, 248, 230, () => TogglePending(feed));
            card.GetComponent<RoundedImage>().cornerRadius = 22;
            var preview = Video(card.transform, "Preview", 3, 3, 242, 136, TextureForFeed(i), 20);
            ODVideoUI.BindVideoTexture(preview, TextureForFeed(i));
            Fill(card.transform, "DemoBadge", 12, 104, 72, 26, Glass, 13);
            Text(card.transform, "SourceLabel", "CAM", 23, 104, 58, 26, 13, true);
            var driver = Text(card.transform, "Driver", DriverName(i), 12, 146, 224, 50, 20, true);
            driver.textWrappingMode = TextWrappingModes.Normal;
            Text(card.transform, "Team", TeamName(i), 12, 199, 224, 26, 16, false, ODDesign.Secondary);
            var mark = Fill(card.transform, "Selection", 202, 12, 32, 32, ODDesign.Gold, 16);
            Icon(mark.transform, "Check", ODVideoIcon.Kind.Check, 7, 7, 18).color = ODDesign.Ink;
            _cameraButtons.Add(card);
        }
    }
    public void TogglePending(int feed)
    {
        if (feed == PrimaryFeed) return;
        if (!_pending.Add(feed)) _pending.Remove(feed);
        UpdatePicker();
    }
    private void UpdatePicker()
    {
        if (_picker == null) return;
        _pickerSubtitle.text = "Chaque caméra se connecte à son flux · Selon disponibilité";
        _picker.transform.Find("CameraCount").GetComponent<TMP_Text>().text = DriverCount + " pilotes" + (_drivers.Count == 0 ? " · Catalogue démo" : "");
        int count = _pending.Count;
        _confirm.GetComponentInChildren<TextMeshProUGUI>().text = _feeds.Count == 0 ? "Ajouter " + count + (count == 1 ? " vue" : " vues") : "Appliquer (" + count + ")";
        _confirm.interactable = count > 0 || _feeds.Count > 0;
        for (int i = 0; i < _cameraButtons.Count; i++)
        {
            var b = _cameraButtons[i]; bool selected = _pending.Contains(i) || PrimaryFeed == i;
            var c = b.colors; c.normalColor = c.selectedColor = selected ? new Color(.68f, .52f, .17f, .60f) : ODDesign.Plateau; b.colors = c;
            b.transform.Find("Selection").gameObject.SetActive(selected); b.interactable = PrimaryFeed != i;
        }
        foreach (var pair in new[] { (_around, _pendingLayout == Layout.Around), (_mosaic, _pendingLayout == Layout.Mosaic) })
        { var c = pair.Item1.colors; c.normalColor = c.selectedColor = pair.Item2 ? new Color(1, 1, 1, .28f) : ODDesign.ButtonFill; pair.Item1.colors = c; }
        _picker.transform.Find("LayoutHint").GetComponent<TextMeshProUGUI>().text = _pendingLayout == Layout.Around
            ? "Les vues suivent la TV. Attrape leur barre pour les détacher."
            : "Une fenêtre latérale groupée. La TV principale reste intacte.";
    }
    public void ConfirmPicker()
    { ApplyViews(_pending.OrderBy(x => x).ToArray(), _pendingLayout); ClosePicker(); }

    public void ApplyViews(IEnumerable<int> feeds, Layout layout)
    {
        _feeds.Clear(); _feeds.AddRange(feeds.Where(i => i >= -1 && i < DriverCount && i != PrimaryFeed).Distinct());
        bool changedLayout = CurrentLayout != layout; CurrentLayout = layout;
        RebuildLayout(changedLayout);
    }
    public void ReattachViews()
    { foreach (var s in _satellites) if (s != null) s.Attach(); if (_groupedView != null) _groupedView.Attach(); RebuildLayout(false); }

    public void ResetViews()
    {
        ClosePicker();
        foreach (var s in _satellites) if (s != null) Remove(s.gameObject);
        _satellites.Clear(); _feeds.Clear(); PrimaryFeed = -1;
        SyncSources();
        if (_groupedView != null) { Remove(_groupedView.gameObject); _groupedView = null; }
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mosaicRoot != null) Clear(mosaicRoot);
        if (mainVideo != null) mainVideo.transform.parent.gameObject.SetActive(true);
        Refresh();
    }

    private void RebuildLayout(bool regroup)
    {
        SyncSources();
        Clear(mosaicRoot);
        mainVideo.transform.parent.gameObject.SetActive(true);
        if (CurrentLayout == Layout.Mosaic && _feeds.Count > 0)
        {
            foreach (var s in _satellites) if (s != null) s.gameObject.SetActive(false);
            if (_groupedView == null)
            {
                if (pilotPrefab == null) return;
                var go = Instantiate(pilotPrefab); go.name = "OverdriveMosaicView";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                _groupedView = go.GetComponent<ODPilotVideoView>();
            }
            _groupedView.InitializeMosaic(this, _feeds);
            _groupedView.offset = new Vector3(-1040, -7.5f, 0); _groupedView.yaw = -12;
            _groupedView.gameObject.SetActive(gameObject.activeInHierarchy);
            _groupedView.GetComponent<Canvas>().worldCamera = Camera.main;
            if (regroup) _groupedView.Attach(); else _groupedView.Follow();
        }
        else
        {
            if (_groupedView != null) _groupedView.gameObject.SetActive(false);
            for (int i = _satellites.Count - 1; i >= 0; i--)
                if (_satellites[i] == null || !_feeds.Contains(_satellites[i].Feed))
                { if (_satellites[i] != null) Remove(_satellites[i].gameObject); _satellites.RemoveAt(i); }
            for (int i = 0; i < _feeds.Count; i++)
            {
                int feed = _feeds[i]; var sat = _satellites.FirstOrDefault(s => s.Feed == feed);
                if (sat == null)
                {
                    if (pilotPrefab == null) continue;
                    var go = Instantiate(pilotPrefab); go.name = "PilotView_" + (feed < 0 ? "Broadcast" : DriverName(feed));
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                    sat = go.GetComponent<ODPilotVideoView>(); sat.Initialize(this, feed); _satellites.Add(sat);
                }
                sat.offset = new Vector3(i % 2 == 0 ? -740 - (i % 4 / 2) * 740 : 1236 + (i % 4 / 2) * 740, -138 - (i / 4) * 444, 0);
                sat.yaw = i % 2 == 0 ? -16 : 16;
                sat.gameObject.SetActive(gameObject.activeInHierarchy); sat.GetComponent<Canvas>().worldCamera = Camera.main;
                if (regroup) sat.Attach(); else sat.Follow();
            }
        }
        Refresh();
    }
    public void Promote(ODPilotVideoView satellite)
    {
        int i = _feeds.IndexOf(satellite.Feed); if (i < 0) return;
        int previous = PrimaryFeed; PrimaryFeed = satellite.Feed; _feeds[i] = previous;
        satellite.SetFeed(previous); SyncSources(); Refresh();
    }
    public void RemoveView(ODPilotVideoView satellite)
    { _feeds.Remove(satellite.Feed); _satellites.Remove(satellite); Remove(satellite.gameObject); RebuildLayout(false); }

    private void OnEnable() { if (_bound) RebuildLayout(false); }
    private void LateUpdate()
    {
        if (!PickerVisible) return;
        _picker.transform.localScale = transform.lossyScale;
        _picker.transform.SetPositionAndRotation(transform.TransformPoint(new Vector3(40, -26, -45)), transform.rotation);
    }
    private void OnDisable()
    { ClosePicker(); if (optionsPanel != null) optionsPanel.SetActive(false); foreach (var s in _satellites) if (s != null) s.gameObject.SetActive(false); if (_groupedView != null) _groupedView.gameObject.SetActive(false); }
    private void OnDestroy()
    {
        if (_picker != null) Remove(_picker);
        if (_groupedView != null) Remove(_groupedView.gameObject);
        foreach (var s in _satellites) if (s != null) Remove(s.gameObject);
    }
}
