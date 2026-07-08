/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HomeNavController - Keeps the home mini-window title in sync with the
 ## selected nav tab, and shows/wires the championship selector buttons.
 ##
 */

using UnityEngine;

/// <summary>
/// Glue between the persistent bottom ODNavBar and the floating home ODCard.
/// The card's content area is empty on every tab except "Championnats",
/// where 3 selector buttons (F1/WEC/MotoGP) appear and open the
/// ChampionshipPageController with the picked championship's full name.
/// </summary>
public class HomeNavController : MonoBehaviour
{
    private const int ChampionshipsTabIndex = 1;

    [Header("References — wired by HomeNavBuilder")]
    public ODNavBar   navBar;
    public ODCard     homeCard;
    public GameObject championshipButtons;

    private void Awake()
    {
        // A World Space canvas needs its worldCamera set for the
        // GraphicRaycaster to hit-test pointer/mouse clicks correctly.
        // Never bake a scene camera reference into the prefab itself.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
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

        if (championshipButtons != null)
            championshipButtons.SetActive(index == ChampionshipsTabIndex);
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
