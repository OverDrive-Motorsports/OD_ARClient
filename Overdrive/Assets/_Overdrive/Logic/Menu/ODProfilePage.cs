/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODProfilePage - Profile page of the main menu (nav tab "Profil"), with its
 ## Réglages tab. Mock-up only (ODProfileView): connected to nothing.
 ##
 */

using UnityEngine;

public sealed class ODProfilePage : MonoBehaviour
{
    ODProfileView _view;

    /// <summary>Creates the page next to <paramref name="template"/> (another menu page), with the same size and placement.</summary>
    public static ODProfilePage Create(RectTransform template)
    {
        var go = new GameObject("ProfilePage", typeof(RectTransform));
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

        var page = go.AddComponent<ODProfilePage>();
        page._view = go.AddComponent<ODProfileView>();
        return page;
    }

    void OnEnable()
    {
        ODPassthroughBlur.Acquire(this);   // frosted room behind the glass (Quest 3, camera permission)
        if (_view == null) _view = GetComponent<ODProfileView>();
        _view.Build();
    }

    void OnDisable() => ODPassthroughBlur.Release(this);
}
