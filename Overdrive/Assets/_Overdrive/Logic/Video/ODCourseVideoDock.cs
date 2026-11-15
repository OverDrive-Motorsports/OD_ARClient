using UnityEngine;

/// <summary>Places the independent TV once, then leaves its grab handle in control.</summary>
public sealed class ODCourseVideoDock : MonoBehaviour
{
    public RectTransform courseWindow;
    public Vector3 localOffset = new Vector3(-1524, 266, 0);
    public float initialYaw = -12f;

    private bool _placed;

    private void OnEnable() => PlaceIfNeeded();

    public void PlaceIfNeeded()
    {
        if (!_placed) Align();
    }

    public void Align()
    {
        if (courseWindow == null) return;
        transform.localScale = courseWindow.lossyScale;
        var rect = (RectTransform)transform;
        var center = (Vector3)rect.rect.center;
        var worldCenter = courseWindow.TransformPoint(localOffset + center);
        var rotation = courseWindow.rotation * Quaternion.Euler(0, initialYaw, 0);
        transform.SetPositionAndRotation(worldCenter - rotation * Vector3.Scale(center, courseWindow.lossyScale), rotation);
        _placed = true;
    }
}
