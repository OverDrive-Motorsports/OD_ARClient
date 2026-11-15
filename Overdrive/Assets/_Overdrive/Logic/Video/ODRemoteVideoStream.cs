using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// One Dev broadcast endpoint and one native decoder/texture. Never falls back to a local clip.
/// Generation checks discard late HTTP replies after hiding, switching session or disposing a view.
/// Only the gateway JSON request carries the gateway token, never the media URL request.
/// </summary>
public sealed class ODRemoteVideoStream : IDisposable
{
    public VideoPlayer Player { get; }
    public bool Ready => !_disposed && Player != null && Player.isPrepared && _ready;
    public bool Loading { get; private set; }
    public bool Requested { get; private set; }
    public bool Finished { get; private set; }
    public string Status { get; private set; } = "Vidéo indisponible";
    public Texture Texture => Ready ? Player.targetTexture : null;
    public event Action Changed;
    private readonly bool _ownsPlayer;
    private RenderTexture _ownedTexture;
    private bool _disposed, _ready, _play;
    private int _version;
    private float _prepareStarted = -1;
    private float _volume;
    private double _resumeTime;

    public ODRemoteVideoStream(VideoPlayer player, bool ownsPlayer)
    {
        Player = player; _ownsPlayer = ownsPlayer;
        Player.playOnAwake = false;
        Player.Stop(); Player.clip = null; Player.source = VideoSource.Url; Player.url = "";
        Player.renderMode = VideoRenderMode.RenderTexture;
        Player.audioOutputMode = VideoAudioOutputMode.Direct;
        Player.controlledAudioTrackCount = 1;
        Player.prepareCompleted += Prepared;
        Player.errorReceived += Failed;
        Player.loopPointReached += Ended;
    }

    public async Task LoadAsync(string sessionId, int? driverNumber = null)
    {
        if (_disposed || Player == null) return;
        int version = ++_version;
        Requested = true; Loading = false; Finished = false; _ready = false; _prepareStarted = -1;
        Player.Stop(); Player.clip = null; Player.source = VideoSource.Url; Player.url = "";
        if (string.IsNullOrWhiteSpace(sessionId))
        { Unavailable("Vidéo indisponible · Aucune session sélectionnée"); return; }
        Loading = true; Status = "Chargement de la vidéo…"; Changed?.Invoke();
        try
        {
            string url;
            if (driverNumber.HasValue)
            {
                if (driverNumber.Value <= 0) { Unavailable(); return; }
                var reply = await ODBackendApi.GetDriverBroadcast(sessionId, driverNumber.Value);
                if (!Current(version)) return;
                if (reply == null || reply.session_id != sessionId || reply.driver_number != driverNumber.Value)
                { Unavailable(); return; }
                url = reply.broadcast_url;
            }
            else
            {
                var reply = await ODBackendApi.GetSessionBroadcast(sessionId);
                if (!Current(version)) return;
                if (reply == null || reply.sessionId != sessionId) { Unavailable(); return; }
                url = reply.broadcastUrl;
            }
            if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var address) ||
                (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
            { Unavailable(); return; }
            if (Player.targetTexture == null)
            {
                _ownedTexture = new RenderTexture(960, 540, 0) { name = "RemoteBroadcastTexture" };
                _ownedTexture.Create(); Player.targetTexture = _ownedTexture;
            }
            Player.url = address.AbsoluteUri;
            _prepareStarted = Time.realtimeSinceStartup;
            Player.Prepare();
        }
        catch (Exception)
        {
            // Do not log response bodies, gateway tokens or signed media URLs.
            if (Current(version)) Unavailable();
        }
    }

    private bool Current(int version) => !_disposed && Player != null && version == _version;
    private void Prepared(VideoPlayer player)
    {
        if (_disposed || !Loading) return;
        _ready = true; Loading = false; _prepareStarted = -1; Status = "";
        if (_resumeTime > 0 && player.canSetTime) player.time = _resumeTime;
        SetVolume(_volume);
        if (_play) player.Play();
        Changed?.Invoke();
    }
    private void Failed(VideoPlayer player, string error) { if (!_disposed) Unavailable(); }
    private void Ended(VideoPlayer player) { _play = false; Finished = true; Changed?.Invoke(); }
    private void Unavailable(string message = "Vidéo indisponible · Réessayer avec Lecture")
    {
        _ready = false; Loading = false; _prepareStarted = -1; Status = message;
        if (Player != null) { Player.Stop(); Player.url = ""; }
        Changed?.Invoke();
    }
    public void Tick()
    {
        if (Loading && _prepareStarted >= 0 && Time.realtimeSinceStartup - _prepareStarted > 20)
            Unavailable();
    }
    public void SetPlayback(bool play)
    {
        _play = play;
        if (!Ready) return;
        if (play && Finished)
        {
            Finished = false; _resumeTime = 0;
            if (Player.canSetTime) Player.time = 0;
        }
        if (play) Player.Play(); else Player.Pause();
    }
    public void SetVolume(float value)
    {
        _volume = Mathf.Clamp01(value);
        if (!Ready) return;
        int tracks = Mathf.Min((int)Player.controlledAudioTrackCount, (int)Player.audioTrackCount);
        for (ushort i = 0; i < tracks; i++) Player.SetDirectAudioVolume(i, _volume);
    }
    public void Suspend()
    {
        if (_disposed) return;
        ++_version; _play = false; _prepareStarted = -1;
        if (Ready)
        {
            _resumeTime = Player.length > 0 && Player.time >= Player.length - .1 ? 0 : Player.time;
            Player.Pause();
        }
        // Reopening rechecks the endpoint, even when the previous media URL was valid.
        Loading = false; Requested = false;
    }
    public void Reset()
    {
        Suspend(); _resumeTime = 0; _ready = false; Finished = false; Status = "Vidéo indisponible";
        if (Player != null) { Player.Stop(); Player.url = ""; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        Suspend(); _disposed = true;
        if (Player != null)
        {
            Player.prepareCompleted -= Prepared; Player.errorReceived -= Failed; Player.loopPointReached -= Ended;
            Player.Stop();
            if (_ownedTexture != null) Player.targetTexture = null;
            if (_ownsPlayer) UnityEngine.Object.Destroy(Player.gameObject);
        }
        if (_ownedTexture != null) { _ownedTexture.Release(); UnityEngine.Object.Destroy(_ownedTexture); }
        Changed = null;
    }
}
