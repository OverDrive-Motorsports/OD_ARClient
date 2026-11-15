using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction.Surfaces;
using System.Collections.Generic;
using System.Linq;

/// <summary>A sibling root canvas: follows the TV until its own existing grab bar is used.</summary>
public sealed class ODPilotVideoView : MonoBehaviour
{
    public bool Attached { get; private set; } = true;
    public int Feed { get; private set; }
    public ODVideoPlayerView Owner { get; private set; }
    public bool IsMosaic { get; private set; }
    public RectTransform MosaicContent { get; private set; }
    private int _mosaicCount;
    private int[] _mosaicFeeds;
    private Texture _mosaicTexture;
    public Vector3 offset;
    public float yaw;
    private TextMeshProUGUI _caption;
    private WindowHandle _handle;
    private bool _bound;
    private RawImage _picture;
    private TextMeshProUGUI _mediaStatus;
    private readonly Dictionary<int, RawImage> _tiles = new Dictionary<int, RawImage>();
    private readonly Dictionary<int, TextMeshProUGUI> _tileStatus = new Dictionary<int, TextMeshProUGUI>();

    public void Initialize(ODVideoPlayerView owner, int feed)
    {
        Bind(owner); Feed = feed; IsMosaic = false;
        ClearWindowContent();
        Resize(new Vector2(704, 396));
        _picture = ODVideoUI.Video(transform, "PilotVideo", 0, 0, 704, 396, owner.TextureForFeed(feed), 26);
        _mediaStatus = ODVideoUI.Text(transform, "VideoUnavailable", "Vidéo indisponible", 32, 136, 640, 108, 24, true);
        _mediaStatus.alignment = TextAlignmentOptions.Center;
        _mediaStatus.textWrappingMode = TextWrappingModes.Normal;
        ODVideoUI.Fill(transform, "Identity", 16, 16, 440, 48, ODVideoUI.Glass, 24);
        _caption = ODVideoUI.Text(transform, "IdentityLabel", owner.FeedName(feed), 30, 16, 412, 48, 22, true);
        _caption.enableAutoSizing = true; _caption.fontSizeMin = 16; _caption.fontSizeMax = 22;
        ODVideoUI.IconButton(transform, "MakeMain", ODVideoIcon.Kind.Swap, 526, 16, 48, () => Owner.Promote(this));
        var mute = ODVideoUI.IconButton(transform, "MutedDemo", ODVideoIcon.Kind.Mute, 584, 16, 48);
        mute.interactable = false;
        ODVideoUI.IconButton(transform, "CloseView", ODVideoIcon.Kind.Close, 642, 16, 48, () => Owner.RemoveView(this));
        ODVideoUI.Fill(transform, "Telemetry", 16, 328, 492, 52, ODVideoUI.Glass, 26);
        ODVideoUI.Text(transform, "TelemetryLabel", "-- km/h    ·    Rapport --    ·    Démo", 34, 328, 460, 52, 19);
        RefreshCaption(); RefreshMedia();
    }

    private void Bind(ODVideoPlayerView owner)
    {
        Owner = owner;
        if (!_bound)
        {
            _bound = true;
            _handle = GetComponent<WindowHandle>();
            if (_handle != null) _handle.GrabStarted += Detach;
        }
    }

    private void ClearWindowContent()
    {
        _picture = null; _mediaStatus = null; _tiles.Clear(); _tileStatus.Clear();
        // Reconfiguring a grouped view must not destroy the handle already created by Start.
        for (int i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name != "WindowHandlePill") ODVideoUI.Remove(transform.GetChild(i).gameObject);
    }
    private void Resize(Vector2 size)
    {
        var rect = (RectTransform)transform; rect.sizeDelta = size;
        var bounds = GetComponent<BoundsClipper>();
        if (bounds != null) { bounds.Position = new Vector3(rect.rect.center.x, rect.rect.center.y, 0); bounds.Size = new Vector3(size.x, size.y, .01f); }
    }
    public void InitializeMosaic(ODVideoPlayerView owner, IEnumerable<int> feeds)
    {
        int[] selected = feeds.ToArray();
        Bind(owner);
        if (IsMosaic && _mosaicFeeds != null && _mosaicFeeds.SequenceEqual(selected) && _mosaicTexture == owner.VideoTexture) return;
        IsMosaic = true; Feed = -2; _mosaicCount = selected.Length; _mosaicFeeds = selected; _mosaicTexture = owner.VideoTexture;
        ClearWindowContent(); Resize(new Vector2(1000, 660));
        _caption = ODVideoUI.Text(transform, "IdentityLabel", "", 24, 16, 850, 48, 26, true);
        ODVideoUI.IconButton(transform, "CloseView", ODVideoIcon.Kind.Close, 934, 16, 48, () => Owner.ApplyViews(new int[0], ODVideoPlayerView.Layout.Mosaic));
        MosaicContent = ODVideoUI.Rect(transform, "GroupedCameras", 18, 84, 964, 558);
        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(selected.Length)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(selected.Length / (float)columns));
        float w = (964 - (columns - 1) * 12) / columns, h = (558 - (rows - 1) * 12) / rows;
        for (int i = 0; i < selected.Length; i++)
        {
            var picture = ODVideoUI.Video(MosaicContent, "Tile" + i, i % columns * (w + 12), i / columns * (h + 12), w, h, owner.TextureForFeed(selected[i]), 18);
            picture.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ODVideoUI.Fill(picture.transform.parent, "CaptionGlass", 6, 6, w - 12, 32, ODVideoUI.Glass, 16);
            var label = ODVideoUI.Text(picture.transform.parent, "Caption", owner.FeedName(selected[i]), 14, 6, w - 28, 32, Mathf.Min(18, w / 14), true);
            label.enableAutoSizing = true; label.fontSizeMin = 9; label.fontSizeMax = Mathf.Min(18, w / 14);
            var state = ODVideoUI.Text(picture.transform.parent, "VideoUnavailable", "Vidéo indisponible", 12, h * .35f, w - 24, h * .5f, Mathf.Min(22, w / 14));
            state.alignment = TextAlignmentOptions.Center; state.textWrappingMode = TextWrappingModes.Normal;
            state.enableAutoSizing = true; state.fontSizeMin = 9; state.fontSizeMax = Mathf.Min(22, w / 14);
            _tiles[selected[i]] = picture; _tileStatus[selected[i]] = state;
        }
        RefreshCaption(); RefreshMedia();
    }

    public void SetFeed(int feed) { Feed = feed; RefreshCaption(); RefreshMedia(); }
    public void RefreshMedia()
    {
        if (Owner == null) return;
        if (IsMosaic)
        {
            foreach (var pair in _tiles)
            {
                var texture = Owner.TextureForFeed(pair.Key);
                ODVideoUI.BindVideoTexture(pair.Value, texture);
                var state = _tileStatus[pair.Key];
                if (state != null) { state.text = Owner.StatusForFeed(pair.Key); state.gameObject.SetActive(texture == null); }
            }
        }
        else
        {
            var texture = Owner.TextureForFeed(Feed);
            ODVideoUI.BindVideoTexture(_picture, texture);
            if (_mediaStatus != null) { _mediaStatus.text = Owner.StatusForFeed(Feed); _mediaStatus.gameObject.SetActive(texture == null); }
        }
    }
    public void Detach() { Attached = false; RefreshCaption(); }
    public void Attach() { Attached = true; Follow(); RefreshCaption(); }
    private void RefreshCaption()
    {
        if (_caption != null && Owner != null) _caption.text = (IsMosaic ? "Mosaïque · " + _mosaicCount + " pilotes" : Owner.FeedName(Feed)) + (Attached ? "" : " · libre");
    }
    public void Follow()
    {
        if (!Attached || Owner == null || !Owner.gameObject.activeInHierarchy) return;
        transform.localScale = Owner.transform.lossyScale;
        var rotation = Owner.transform.rotation * Quaternion.Euler(0, yaw, 0);
        var center = (Vector3)((RectTransform)transform).rect.center;
        var desiredCenter = Owner.transform.TransformPoint(offset + center);
        transform.SetPositionAndRotation(desiredCenter - rotation * Vector3.Scale(center, Owner.transform.lossyScale), rotation);
    }
    private void LateUpdate() => Follow();
    private void OnDestroy()
    {
        if (_handle != null) _handle.GrabStarted -= Detach;
    }
}
