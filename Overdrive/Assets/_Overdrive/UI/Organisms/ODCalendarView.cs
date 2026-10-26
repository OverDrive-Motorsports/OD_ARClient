/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODCalendarView - Display side of the Calendar page: for now an empty
 ## visionOS glass window with its title only. Built at runtime; display only.
 ##
 */

using UnityEngine;

public class ODCalendarView : MonoBehaviour
{
    bool _built;

    public void Build()
    {
        if (_built) return;
        _built = true;
        var root = (RectTransform)transform;

        var surface = GetComponent<ODHomepageSurface>();
        if (surface == null) surface = gameObject.AddComponent<ODHomepageSurface>();
        surface.color = Color.white;
        surface.raycastTarget = true;   // the window swallows rays like the other pages
        ODChampionshipTheme.ApplyBackground(surface, ODChampionshipTheme.NeutralPalette);

        // Design header (Canva 02.3): caps line above the 64-px title; the month grid comes later.
        ODDesign.Caps(root, "Caps", "Calendrier · Saison 2026", ODDesign.WindowPadSide, ODDesign.WindowPadTop, 900f, ODDesign.PageCaps);
        ODDesign.Text(root, "Heading", "Calendrier", ODDesign.WindowPadSide, ODDesign.WindowPadTop + 37f, 900f, 72f, ODDesign.TitleXL, ODDesign.Weight.Bold);
    }
}
