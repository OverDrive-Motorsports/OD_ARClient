using UnityEngine;
using UnityEngine.UI;

/// <summary>Independent navigation window. Input gestures may call Show, Hide or Toggle later.</summary>
[RequireComponent(typeof(Canvas))]
public class FloatingNavigation : MonoBehaviour
{
    public OverdriveMainMenu menu;
    public Canvas contentWindow;
    public Button racesButton;
    public Button rankingsButton;
    public Button profileButton;
    public Button settingsButton;

    private void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
        if (menu != null)
        {
            menu.externalNavigation = true;
            menu.racesButton = racesButton;
            menu.rankingsButton = rankingsButton;
            menu.profileButton = profileButton;
            menu.settingsButton = settingsButton;
        }
        racesButton?.onClick.AddListener(OpenRaces);
        rankingsButton?.onClick.AddListener(OpenRankings);
        profileButton?.onClick.AddListener(OpenProfile);
        settingsButton?.onClick.AddListener(OpenSettings);
    }

    private void OnDestroy()
    {
        if (racesButton) racesButton.onClick.RemoveListener(OpenRaces);
        if (rankingsButton) rankingsButton.onClick.RemoveListener(OpenRankings);
        if (profileButton) profileButton.onClick.RemoveListener(OpenProfile);
        if (settingsButton) settingsButton.onClick.RemoveListener(OpenSettings);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
    public void OpenRaces() => OpenPage(0);
    public void OpenRankings() => OpenPage(1);
    public void OpenProfile() => OpenPage(2);
    public void OpenSettings() => OpenPage(3);

    public void OpenPage(int index)
    {
        if (menu == null || contentWindow == null || index < 0 || index > 3) return;
        contentWindow.gameObject.SetActive(true);
        var group = contentWindow.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 1f;
        menu.SelectNav(index);
    }
}
