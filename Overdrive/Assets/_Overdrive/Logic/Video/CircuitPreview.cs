using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Independent map selector and movable miniature circuit. The miniature can be
/// spun and zoomed (CircuitManipulator: one ray spins, two rays zoom, thumbsticks)
/// and from the visionOS ornament floating under it.
/// </summary>
[RequireComponent(typeof(Canvas), typeof(WindowHandle))]
public sealed class CircuitPreview : MonoBehaviour
{
    [System.Serializable]
    public sealed class Circuit
    {
        public string label;
        public GameObject prefab;
        public Button button;
    }

    public Circuit[] circuits;
    public Transform modelMount;
    public Transform circuitRoot;
    public TMP_Text circuitLabel;
    [Tooltip("Maximum model dimensions in canvas units (1000 units = 1 metre).")]
    public Vector3 modelSpace = new Vector3(700f, 150f, 700f);
    public Vector3 viewingAngles = Vector3.zero;
    public float playerGap = 0.10f;

    public int SelectedIndex { get; private set; } = -1;
    public GameObject CurrentModel { get; private set; }
    private bool _placed;
    private bool _bound;
    private CircuitManipulator _manipulator;

    private ODCircuitOrnament _ornament;

    const float SpinStep = 45f;
    // Ornament placement (constants, not serialized: scene copies would keep stale values).
    // Metres from the circuit centre toward the user, height relative to the tabletop
    // (just below its front edge, under the handle bar, like a visionOS volume ornament),
    // backward tilt, and seconds it stays once the handle bar / ornament stop being aimed at.
    const float ornamentDistance = .46f, ornamentHeight = -.06f, ornamentTilt = 28f, ornamentLinger = 2.5f;

    private CanvasGroup _ornamentGroup;
    private Oculus.Interaction.RayInteractable _ornamentRay;
    private WindowHandle _tabletopHandle;
    private float _ornamentUntil, _ornamentShown;

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (_bound || circuits == null) return;
        _bound = true;
        for (int i = 0; i < circuits.Length; i++)
        {
            int index = i;
            if (circuits[i]?.button != null)
                circuits[i].button.onClick.AddListener(() => SelectCircuit(index));
        }

        if (circuitRoot != null && modelMount != null)
        {
            _manipulator = circuitRoot.GetComponent<CircuitManipulator>();
            if (_manipulator == null) _manipulator = circuitRoot.gameObject.AddComponent<CircuitManipulator>();
            _manipulator.target = modelMount;
            BuildOrnament();
        }
    }

    /// <summary>visionOS ornament under the circuit: spin, zoom slider, reset.</summary>
    private void BuildOrnament()
    {
        if (_ornament != null) return;
        var canvas = ODPointableCanvas.Create("CircuitOrnament", new Vector2(ODCircuitOrnament.Width, ODCircuitOrnament.Height));
        _ornament = ODCircuitOrnament.Build(canvas);
        _ornament.SpinRequested += direction => _manipulator.Spin(direction * SpinStep);
        _ornament.ZoomChanged += value => _manipulator.NormalizedZoom = value;
        _ornament.ResetRequested += () => _manipulator.ResetView(false);
        _ornamentGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        _ornamentRay = canvas.GetComponent<Oculus.Interaction.RayInteractable>();
        _tabletopHandle = circuitRoot.GetComponent<WindowHandle>();
        canvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// visionOS ornament: appears when the circuit's handle bar is aimed at, stays while
    /// it (or the ornament) is aimed at or the circuit is handled, then fades out.
    /// </summary>
    private void LateUpdate()
    {
        if (_ornament == null || circuitRoot == null) return;
        bool rootVisible = circuitRoot.gameObject.activeInHierarchy;
        bool wanted = rootVisible && (
            (_tabletopHandle != null && (_tabletopHandle.IsHovered || _tabletopHandle.IsGrabbed)) ||
            (_ornamentRay != null && _ornament.gameObject.activeSelf && _ornamentRay.State != Oculus.Interaction.InteractableState.Normal) ||
            _ornament.IsDraggingZoom || _manipulator.IsHandled);
        if (wanted) _ornamentUntil = ODTime.Now + ornamentLinger;
        bool show = rootVisible && ODTime.Now < _ornamentUntil;

        float dt = ODTime.Delta;
        _ornamentShown = Mathf.MoveTowards(_ornamentShown, show ? 1f : 0f, dt / (show ? .18f : .30f));
        if (_ornamentShown <= 0f)
        {
            if (_ornament.gameObject.activeSelf) _ornament.gameObject.SetActive(false);
            return;
        }
        if (!_ornament.gameObject.activeSelf) _ornament.gameObject.SetActive(true);

        float eased = ODRuntimeUI.EaseOutCubic(_ornamentShown);
        _ornamentGroup.alpha = eased;
        _ornamentGroup.interactable = _ornamentGroup.blocksRaycasts = show;

        // Under the circuit's front edge, just in front of the handle bar, tilted back to face the eyes.
        Vector3 forward = Vector3.ProjectOnPlane(circuitRoot.forward, Vector3.up);
        if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
        forward.Normalize();
        _ornament.transform.SetPositionAndRotation(
            circuitRoot.position - forward * ornamentDistance + Vector3.up * (ornamentHeight - .02f * (1f - eased)),
            Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(ornamentTilt, 0f, 0f));
        _ornament.transform.localScale = Vector3.one * .001f * Mathf.Lerp(.92f, 1f, eased);
        _ornament.SetZoom(_manipulator.NormalizedZoom);
    }

    private void OnDestroy()
    {
        if (_ornament != null) Destroy(_ornament.gameObject);
    }

    public void ShowBeside(RectTransform player, bool onRight = false)
    {
        if (player == null) return;
        Initialize();
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        var rect = (RectTransform)transform;
        float offset = player.rect.width * Mathf.Abs(player.lossyScale.x) * 0.5f
                     + rect.rect.width * Mathf.Abs(rect.lossyScale.x) * 0.5f + playerGap;
        var center = player.TransformPoint(player.rect.center);
        transform.SetPositionAndRotation(center + player.right * (onRight ? offset : -offset), player.rotation);
        transform.position -= transform.TransformVector(rect.rect.center);
        gameObject.SetActive(true);
        if (circuitRoot != null)
        {
            if (!_placed)
            {
                var head = Camera.main != null ? Camera.main.transform : player;
                var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
                circuitRoot.SetPositionAndRotation(head.position + forward * .8f - Vector3.up * .55f,
                    Quaternion.LookRotation(forward, Vector3.up));
                _placed = true;
            }
            circuitRoot.gameObject.SetActive(true);
        }
        if (CurrentModel == null) SelectCircuit(SelectedIndex < 0 ? 0 : SelectedIndex);
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnEnable() => ODPassthroughBlur.Acquire(this);   // frosted room behind the glass ornament

    private void OnDisable()
    {
        ODPassthroughBlur.Release(this);
        if (_ornament != null) _ornament.gameObject.SetActive(false);
        if (circuitRoot != null) circuitRoot.gameObject.SetActive(false);
    }

    public void SelectCircuit(int index)
    {
        if (circuits == null || index < 0 || index >= circuits.Length ||
            circuits[index] == null || circuits[index].prefab == null || modelMount == null)
            return;
        if (index == SelectedIndex && CurrentModel != null) return;

        var model = Instantiate(circuits[index].prefab, modelMount, false);
        model.name = circuits[index].label + " Miniature";
        model.transform.localRotation = Quaternion.Euler(viewingAngles);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = Vector3.one;
        if (!TryGetMeshBounds(model.transform, modelMount, out Bounds bounds))
        {
            Destroy(model);
            Debug.LogWarning("[Circuits] Model has no displayable mesh: " + circuits[index].label, this);
            return;
        }

        float scale = FitScale(bounds.size, modelSpace);
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = -bounds.center * scale;
        // Keep the bottom of every circuit on the same tabletop plane.
        model.transform.localPosition += Vector3.up * bounds.extents.y * scale;
        if (CurrentModel != null)
        {
            CurrentModel.SetActive(false);
            Destroy(CurrentModel);
        }
        CurrentModel = model;
        SelectedIndex = index;
        if (_manipulator != null)
        {
            _manipulator.SetModelBounds(TryGetMeshBounds(model.transform, modelMount, out Bounds placed) ? placed : (Bounds?)null);
            _manipulator.ResetView(false);
        }
        if (circuitLabel != null) circuitLabel.text = circuits[index].label;
        UpdateButtons();
    }

    public static float FitScale(Vector3 size, Vector3 available)
    {
        return Mathf.Min(Mathf.Max(1f, available.x) / Mathf.Max(0.0001f, size.x),
                         Mathf.Max(1f, available.y) / Mathf.Max(0.0001f, size.y),
                         Mathf.Max(1f, available.z) / Mathf.Max(0.0001f, size.z));
    }

    // Mesh bounds work without Read/Write mesh copies, including Android builds.
    public static bool TryGetMeshBounds(Transform root, Transform relativeTo, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled) continue;
            var meshBounds = filter.sharedMesh.bounds;
            Matrix4x4 matrix = relativeTo.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return found;
    }

    private void UpdateButtons()
    {
        for (int i = 0; i < circuits.Length; i++)
        {
            var button = circuits[i]?.button;
            if (button == null) continue;
            if (button.targetGraphic != null) button.targetGraphic.color = Color.white;
            var colors = button.colors;
            colors.normalColor = i == SelectedIndex
                ? new Color(0.48f, 0.60f, 0.73f, 1f) : new Color(0.22f, 0.28f, 0.36f, 1f);
            colors.highlightedColor = new Color(0.56f, 0.67f, 0.79f, 1f);
            colors.pressedColor = new Color(0.34f, 0.46f, 0.58f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
    }
}
