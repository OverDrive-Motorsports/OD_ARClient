using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>A separately movable, miniature 3D circuit beside the video player.</summary>
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

    private void Awake()
    {
        if (circuits == null) return;
        for (int i = 0; i < circuits.Length; i++)
        {
            int index = i;
            if (circuits[i]?.button != null)
                circuits[i].button.onClick.AddListener(() => SelectCircuit(index));
        }
    }

    public void ShowBeside(RectTransform player)
    {
        if (player == null) return;
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        var rect = (RectTransform)transform;
        float offset = player.rect.width * Mathf.Abs(player.lossyScale.x) * 0.5f
                     + rect.rect.width * Mathf.Abs(rect.lossyScale.x) * 0.5f + playerGap;
        // Standings occupy the right; circuits open on the left.
        transform.SetPositionAndRotation(player.position - player.right * offset, player.rotation);
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

    private void OnDisable()
    {
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
