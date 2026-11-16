using UnityEngine;

/// <summary>
/// Places the independent TV each time it appears: in the centre, in front of the headset, with
/// the main window gliding to its right; when it is hidden the main window glides back to the
/// centre (ODWindowLayout). Its grab handle moves it freely meanwhile.
/// </summary>
public sealed class ODCourseVideoDock : MonoBehaviour
{
    [Tooltip("Race window whose scale the TV takes.")]
    public RectTransform courseWindow;

    private bool _placed;

    private void OnEnable() => PlaceIfNeeded();

    private void OnDisable()
    {
        _placed = false;   // placed again the next time it appears
        ODWindowLayout.CloseCentre((RectTransform)transform);
    }

    /// <summary>Places the TV once per appearance (OnEnable and the TV button both ask).</summary>
    public void PlaceIfNeeded()
    {
        if (!_placed) Align();
    }

    public void Align()
    {
        if (courseWindow != null) transform.localScale = courseWindow.lossyScale;
        ODWindowLayout.OpenAtCentre((RectTransform)transform);
        _placed = true;
    }
}
