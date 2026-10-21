/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODPassthroughBlur - Global service behind the visionOS "liquid glass": reads
 ## the Quest 3 passthrough camera (Meta MRUK PassthroughCameraAccess), blurs it
 ## heavily on the GPU and stitches every frame into a world-anchored panorama
 ## around the user. The OD/UI Glass shader looks the panorama up in the
 ## direction behind each panel, so the frosted room stays in place even where
 ## the camera (about 70° wide, narrower than the display) can't see.
 ##
 */

using Meta.XR;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Reference-counted: each glass page calls Acquire in OnEnable and Release in
/// OnDisable. The camera only streams while at least one page is visible; a
/// short grace period avoids restarting it when switching pages, then it is
/// stopped and its texture freed. Low rate on purpose (10 fps, 320x240): the
/// image is only a soft colour field, and the cameras are shared with hand,
/// controller and world tracking.
/// The panorama outlives the stream: once a frame has been seen, the glass
/// keeps showing the last known room while the camera restarts.
/// Quest 3 / 3S only, with the horizonos.permission.HEADSET_CAMERA permission
/// (asked on first use). When the camera isn't available — Editor, other
/// headsets, permission refused — _ODBlurReady stays 0 and the glass shader
/// uses a neutral room colour instead (frosted, never clear).
/// </summary>
public sealed class ODPassthroughBlur : MonoBehaviour
{
    const string CameraPermission = "horizonos.permission.HEADSET_CAMERA";

    /// <summary>Blur resolution: tiny on purpose, the image only needs to be a soft colour field.</summary>
    const int BlurWidth = 80, BlurHeight = 60;
    static readonly float[] KawaseOffsets = { 0f, 1f, 2f, 3f };

    /// <summary>Panorama: 360° x 180°, about 1.4° per texel (the image is a blurred colour field).</summary>
    const int PanoWidth = 256, PanoHeight = 128;
    const float PanoWeight = .65f;   // share of each new frame: smooths the 10-fps stream and exposure changes

    const int CameraFramerate = 10;
    const float StopDelay = 2f;   // seconds without a glass page before the camera stops

    [Tooltip("Set when the camera texture turns out to be stored top-down on device.")]
    public bool flipY;

    // Globals read by the OD/UI Glass shader.
    static readonly int PanoTexId = Shader.PropertyToID("_ODPanoTex");
    static readonly int PanoOriginId = Shader.PropertyToID("_ODPanoOrigin");
    static readonly int ReadyId = Shader.PropertyToID("_ODBlurReady");
    // Panorama pass inputs.
    static readonly int CamTexId = Shader.PropertyToID("_CamTex");
    static readonly int WorldToLocalId = Shader.PropertyToID("_CamWorldToLocal");
    static readonly int IntrinsicsId = Shader.PropertyToID("_CamIntrinsics");
    static readonly int CropId = Shader.PropertyToID("_CamCrop");
    static readonly int FlipId = Shader.PropertyToID("_CamFlipY");
    static readonly int WeightId = Shader.PropertyToID("_Weight");
    static readonly int OffsetId = Shader.PropertyToID("_Offset");

    static ODPassthroughBlur _instance;
    static readonly System.Collections.Generic.HashSet<Object> _users = new System.Collections.Generic.HashSet<Object>();
    static bool _permissionAsked;
    static bool? _supported;

    /// <summary>Cached: the SDK check goes through JNI on every call.</summary>
    static bool Supported => _supported ?? (_supported = PassthroughCameraAccess.IsSupported).Value;

    PassthroughCameraAccess _camera;
    Material _blur, _pano;
    RenderTexture _half, _ping, _pong;
    RenderTexture _panoFront, _panoBack;
    Transform _trackingSpace, _head;
    bool _hasImage, _hasPano, _rigSearched;
    float _idleTime;

    public static bool IsRunning => _instance != null && _instance._hasImage;

    /// <summary>The glass has a frosted room to show (current or last known).</summary>
    static bool HasPanorama => _instance != null && _instance._hasPano;

    /// <summary>A glass page became visible: make sure the camera streams.</summary>
    public static void Acquire(Object owner)
    {
        if (owner == null) return;
        _users.Add(owner);
        Shader.SetGlobalFloat(ReadyId, HasPanorama ? 1f : 0f);
        if (Application.isEditor || !Supported) return;

        if (_instance == null) Create();
        _instance._idleTime = 0f;
        if (!_instance._camera.enabled) _instance._camera.enabled = true;
    }

    /// <summary>A glass page was hidden; the camera stops once no page needs it.</summary>
    public static void Release(Object owner)
    {
        if (!ReferenceEquals(owner, null)) _users.Remove(owner);   // also called while the page is being destroyed
    }

    static void Create()
    {
        var go = new GameObject(nameof(ODPassthroughBlur));
        go.SetActive(false);   // configure the camera before its OnEnable runs
        DontDestroyOnLoad(go);

        _instance = go.AddComponent<ODPassthroughBlur>();
        var camera = go.AddComponent<PassthroughCameraAccess>();
        camera.enabled = false;   // MaxFramerate refuses changes while the component is enabled, even on an inactive object
        camera.CameraPosition = PassthroughCameraAccess.CameraPositionType.Left;
        camera.RequestedResolution = new Vector2Int(320, 240);   // smallest stream
        camera.MaxFramerate = CameraFramerate;
        camera.enabled = true;   // OnEnable runs when the object is activated below
        _instance._camera = camera;

#if UNITY_ANDROID && !UNITY_EDITOR
        // PassthroughCameraAccess waits for the permission on its own once granted.
        if (!_permissionAsked && !Permission.HasUserAuthorizedPermission(CameraPermission))
            Permission.RequestUserPermission(CameraPermission);
#endif
        _permissionAsked = true;
        go.SetActive(true);
    }

    void Awake()
    {
        var shader = Resources.Load<Shader>("Shaders/ODGlassBlur");
        if (shader != null) _blur = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        shader = Resources.Load<Shader>("Shaders/ODGlassPanorama");
        if (shader != null) _pano = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        Shader.SetGlobalFloat(ReadyId, 0f);
    }

    void LateUpdate()
    {
        if (_camera == null || _blur == null || _pano == null) return;
        FindRig();
        if (_hasPano) PublishOrigin();

        // Nobody shows glass: after a grace period, stop the stream (frees the camera and its texture).
        if (_users.Count == 0)
        {
            _idleTime += Time.unscaledDeltaTime;
            if (_idleTime >= StopDelay && _camera.enabled) StopCamera();
            return;
        }

        // Camera starting or paused: the glass keeps the last known panorama.
        if (!_camera.enabled || !_camera.IsPlaying) return;

        // Work only when a new image arrived (10 fps): blur it and stitch it into the panorama.
        if (!_camera.IsUpdatedThisFrame) return;
        var source = _camera.GetTexture();
        if (source == null) return;

        Blur(source);
        Stitch();
        _hasImage = true;
        if (!_hasPano)
        {
            _hasPano = true;
            PublishOrigin();
            Shader.SetGlobalFloat(ReadyId, 1f);
        }
    }

    void StopCamera()
    {
        _camera.enabled = false;   // PassthroughCameraAccess stops the stream and destroys its texture
        _hasImage = false;
        ReleaseBlurTargets();      // the panorama stays: the glass keeps the last known room
    }

    void Blur(Texture source)
    {
        EnsureTargets();
        Graphics.Blit(source, _half);   // 320x240 -> 160x120
        Graphics.Blit(_half, _ping);    // -> 80x60
        foreach (float offset in KawaseOffsets)
        {
            _blur.SetFloat(OffsetId, offset);
            Graphics.Blit(_ping, _pong, _blur);
            (_ping, _pong) = (_pong, _ping);
        }
    }

    /// <summary>
    /// Projects the blurred frame onto the panorama (same maths as
    /// PassthroughCameraAccess.WorldToViewportPoint, per panorama texel) and
    /// blends it over the directions the camera sees; the others keep their value.
    /// </summary>
    void Stitch()
    {
        Pose pose = _camera.GetCameraPose();
        // The headset pose is in tracking space; move it into world space if the rig isn't at the origin.
        if (_trackingSpace != null) pose = new Pose(_trackingSpace.TransformPoint(pose.position), _trackingSpace.rotation * pose.rotation);

        var intrinsics = _camera.Intrinsics;
        _pano.SetTexture(CamTexId, _ping);
        _pano.SetMatrix(WorldToLocalId, Matrix4x4.Rotate(Quaternion.Inverse(pose.rotation)));
        _pano.SetVector(IntrinsicsId, new Vector4(intrinsics.FocalLength.x, intrinsics.FocalLength.y,
            intrinsics.PrincipalPoint.x, intrinsics.PrincipalPoint.y));

        // Sensor region actually covered by the current resolution (PassthroughCameraAccess.CalcSensorCropRegion).
        Vector2 sensor = intrinsics.SensorResolution;
        Vector2 current = _camera.CurrentResolution;
        Vector2 scale = new Vector2(current.x / sensor.x, current.y / sensor.y);
        scale /= Mathf.Max(scale.x, scale.y);
        _pano.SetVector(CropId, new Vector4(sensor.x * (1f - scale.x) * .5f, sensor.y * (1f - scale.y) * .5f,
            sensor.x * scale.x, sensor.y * scale.y));
        _pano.SetFloat(FlipId, flipY ? 1f : 0f);
        _pano.SetFloat(WeightId, PanoWeight);

        EnsurePanorama();
        Graphics.Blit(_panoFront, _panoBack, _pano);
        (_panoFront, _panoBack) = (_panoBack, _panoFront);
        Shader.SetGlobalTexture(PanoTexId, _panoFront);
    }

    /// <summary>The glass looks the room up from the user's head, every frame.</summary>
    void PublishOrigin()
    {
        var head = _head != null ? _head : Camera.main != null ? Camera.main.transform : null;
        if (head != null) Shader.SetGlobalVector(PanoOriginId, head.position);
    }

    void FindRig()
    {
        if (_rigSearched) return;
        _rigSearched = true;
        var rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig == null) return;
        _trackingSpace = rig.trackingSpace;
        _head = rig.centerEyeAnchor;
    }

    void EnsureTargets()
    {
        if (_ping != null) return;
        _half = NewTarget(BlurWidth * 2, BlurHeight * 2);
        _ping = NewTarget(BlurWidth, BlurHeight);
        _pong = NewTarget(BlurWidth, BlurHeight);
    }

    void EnsurePanorama()
    {
        if (_panoFront != null) return;
        _panoFront = NewPanorama();
        _panoBack = NewPanorama();
    }

    static RenderTexture NewTarget(int width, int height)
    {
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        rt.Create();
        return rt;
    }

    /// <summary>Wraps around in yaw; mipmapped so the shader can read the average colour (top mip).</summary>
    static RenderTexture NewPanorama()
    {
        var rt = new RenderTexture(PanoWidth, PanoHeight, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            useMipMap = true,
            autoGenerateMips = true,
        };
        rt.Create();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);   // coverage 0 everywhere: nothing seen yet
        RenderTexture.active = previous;
        return rt;
    }

    void ReleaseBlurTargets()
    {
        DestroyTarget(ref _half);
        DestroyTarget(ref _ping);
        DestroyTarget(ref _pong);
    }

    static void DestroyTarget(ref RenderTexture rt)
    {
        if (rt == null) return;
        rt.Release();
        Destroy(rt);
        rt = null;
    }

    void OnDestroy()
    {
        Shader.SetGlobalFloat(ReadyId, 0f);
        Shader.SetGlobalTexture(PanoTexId, Texture2D.blackTexture);
        ReleaseBlurTargets();
        DestroyTarget(ref _panoFront);
        DestroyTarget(ref _panoBack);
        if (_blur != null) Destroy(_blur);
        if (_pano != null) Destroy(_pano);
        if (_instance == this) _instance = null;
    }
}
