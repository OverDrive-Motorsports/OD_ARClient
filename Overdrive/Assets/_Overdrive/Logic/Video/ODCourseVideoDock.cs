using UnityEngine;

/// <summary>Places the independent TV once, then leaves its grab handle in control.</summary>
public sealed class ODCourseVideoDock : MonoBehaviour
{
    public RectTransform courseWindow;
    public Vector3 localOffset = new Vector3(-1524, 266, 0);

    private bool _placed;

    private void OnEnable() => PlaceIfNeeded();

    public void PlaceIfNeeded()
    {
        if (!_placed) Align();
    }

    public void Align()
    {
        if (courseWindow == null) return;
        transform.SetPositionAndRotation(courseWindow.TransformPoint(localOffset), courseWindow.rotation);
        transform.localScale = courseWindow.lossyScale;
        _placed = true;
    }
}
