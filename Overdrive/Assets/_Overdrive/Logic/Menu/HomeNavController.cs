/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeNavController - Keeps the home mini-window title in sync with the
 ## selected nav tab, and shows/wires the championship selector buttons and
 ## the Profil/Réglages area.
 ##
 */

using System.Collections;
using UnityEngine;

/// <summary>
/// Glue between the persistent bottom ODNavBar and the floating home ODCard.
/// The card's content area is empty except on 2 tabs: "Championnats" shows 3
/// selector buttons (F1/WEC/MotoGP) that open ChampionshipPageController;
/// "Profil" shows the profileArea built/driven by ProfileSectionController.
/// </summary>
public class HomeNavController : MonoBehaviour
{
    private const int ChampionshipsTabIndex = 1;
    private const int ProfileTabIndex = 4;

    [Header("References — wired by HomeNavBuilder")]
    public ODNavBar navBar;
    public ODCard homeCard;
    public GameObject championshipButtons;
    public GameObject profileArea;

    private CanvasGroup _championshipGroup;
    private CanvasGroup _profileGroup;
    private Coroutine _championshipRoutine;
    private Coroutine _profileRoutine;

    private void Awake()
    {
        // A World Space canvas needs its worldCamera set for the
        // GraphicRaycaster to hit-test pointer/mouse clicks correctly.
        // Never bake a scene camera reference into the prefab itself.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;

        if (championshipButtons != null) _championshipGroup = GetOrAddCanvasGroup(championshipButtons);
        if (profileArea != null) _profileGroup = GetOrAddCanvasGroup(profileArea);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        return cg != null ? cg : go.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (navBar != null)
        {
            navBar.OnTabChanged.AddListener(OnTabChanged);
            OnTabChanged(navBar.selectedIndex);
        }

        // Buttons must be wired at RUNTIME: editor-time AddListener calls are
        // not serialized into the prefab, so they'd be dead in a build.
        WireChampionshipButtons();
    }

    private void OnTabChanged(int index)
    {
        if (navBar == null || homeCard == null) return;
        if (index < 0 || index >= navBar.items.Count) return;
        homeCard.SetTitle(navBar.items[index].label);

        SetSectionVisible(championshipButtons, _championshipGroup, ref _championshipRoutine, index == ChampionshipsTabIndex);
        SetSectionVisible(profileArea, _profileGroup, ref _profileRoutine, index == ProfileTabIndex);
    }

    /// <summary>Cross-fades a tab section in/out instead of an instant SetActive cut. No-op if already in the requested state.</summary>
    private void SetSectionVisible(GameObject section, CanvasGroup group, ref Coroutine routine, bool visible)
    {
        if (section == null) return;
        if (section.activeSelf == visible) return;

        if (routine != null) StopCoroutine(routine);

        if (visible)
        {
            section.SetActive(true);
            routine = StartCoroutine(UITransitions.FadeScaleIn(group, section.transform));
        }
        else
        {
            routine = StartCoroutine(HideSection(section, group));
        }
    }

    private IEnumerator HideSection(GameObject section, CanvasGroup group)
    {
        yield return UITransitions.FadeScaleOut(group, section.transform);
        section.SetActive(false);
    }

    private void WireChampionshipButtons()
    {
        if (championshipButtons == null) return;

        var championships = ODChampionshipMockData.Mocks;
        ODButton[] buttons = championshipButtons.GetComponentsInChildren<ODButton>(true);

        for (int i = 0; i < buttons.Length && i < championships.Count; i++)
        {
            var data = championships[i];
            buttons[i].OnClick.RemoveAllListeners();
            buttons[i].OnClick.AddListener(() =>
            {
                if (ChampionshipPageController.Instance != null)
                    ChampionshipPageController.Instance.Open(data);
                else
                    Debug.LogWarning("[HomeNavController] No ChampionshipPageController in scene.");
            });
        }
    }
}
