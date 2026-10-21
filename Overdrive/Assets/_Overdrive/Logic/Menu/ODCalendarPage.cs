/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODCalendarPage - Calendar page of the main menu (nav tab "Calendrier").
 ## Empty for now: the window and its title only (ODCalendarView).
 ##
 */

using UnityEngine;

public sealed class ODCalendarPage : MonoBehaviour
{
    ODCalendarView _view;

    /// <summary>Creates the page next to <paramref name="template"/> (another menu page), with the same size and placement.</summary>
    public static ODCalendarPage Create(RectTransform template)
    {
        var go = new GameObject("CalendarPage", typeof(RectTransform));
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(template.parent, false);
        rect.anchorMin = template.anchorMin;
        rect.anchorMax = template.anchorMax;
        rect.pivot = template.pivot;
        rect.anchoredPosition = template.anchoredPosition;
        rect.sizeDelta = template.sizeDelta;
        rect.localScale = template.localScale;
        rect.SetSiblingIndex(template.GetSiblingIndex() + 1);

        var page = go.AddComponent<ODCalendarPage>();
        page._view = go.AddComponent<ODCalendarView>();
        return page;
    }

    void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        if (_view == null) _view = GetComponent<ODCalendarView>();
        _view.Build();
    }

    void OnDisable() => ODPassthroughBlur.Release(this);
}
