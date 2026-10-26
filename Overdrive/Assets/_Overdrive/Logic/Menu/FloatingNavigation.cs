using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Independent navigation window (left wrist). Four tabs — Home, Championships,
/// Calendar, Profile — dressed as a visionOS tab bar (ODTabBar). The prefab's
/// four buttons are reused in order; their serialized names predate the tabs.
/// Input gestures may call Show, Hide or Toggle later.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class FloatingNavigation : MonoBehaviour
{
    public OverdriveMainMenu menu;
    public Canvas contentWindow;
    [FormerlySerializedAs("racesButton")]
    [Tooltip("Tab 1 — Home.")]
    public Button homepageButton;
    [FormerlySerializedAs("rankingsButton")]
    [Tooltip("Tab 2 — Championships (category directory, sport page, replays).")]
    public Button categoriesButton;
    [Tooltip("Tab 3 — Calendar.")]
    public Button profileButton;
    [Tooltip("Tab 4 — Profile.")]
    public Button settingsButton;

    static readonly ODTabBar.Tab[] Tabs =
    {
        new ODTabBar.Tab("Home",         ODNavigationIcon.Symbol.Homepage),
        new ODTabBar.Tab("Championnats", ODNavigationIcon.Symbol.Trophy),
        new ODTabBar.Tab("Calendrier",   ODNavigationIcon.Symbol.Calendar),
        new ODTabBar.Tab("Profil",       ODNavigationIcon.Symbol.Profile),
    };

    private ODTabBar _tabBar;

    private void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
        if (menu != null)
        {
            // The tab bar owns the active-tab visuals; the menu must not recolour these buttons.
            menu.externalNavigation = true;
            menu.ReleaseNavButtons();
        }
        homepageButton?.onClick.AddListener(OpenHomepage);
        categoriesButton?.onClick.AddListener(OpenCategories);
        profileButton?.onClick.AddListener(OpenCalendar);
        settingsButton?.onClick.AddListener(OpenProfile);

        var bar = homepageButton != null ? homepageButton.transform.parent as RectTransform : null;
        if (bar != null)
            _tabBar = ODTabBar.Attach(bar, new[] { homepageButton, categoriesButton, profileButton, settingsButton }, Tabs);
    }

    private void OnEnable() => ODPassthroughBlur.Acquire(this);   // frosted room behind the glass bar (Quest 3, camera permission)
    private void OnDisable() => ODPassthroughBlur.Release(this);

    private void Update()
    {
        if (_tabBar != null && menu != null) _tabBar.SetActive(menu.ActiveTab);
    }

    private void OnDestroy()
    {
        if (homepageButton) homepageButton.onClick.RemoveListener(OpenHomepage);
        if (categoriesButton) categoriesButton.onClick.RemoveListener(OpenCategories);
        if (profileButton) profileButton.onClick.RemoveListener(OpenCalendar);
        if (settingsButton) settingsButton.onClick.RemoveListener(OpenProfile);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);

    public void OpenHomepage()
    {
        if (!OpenWindow()) return;
        menu.ShowHomepage();
    }

    public void OpenCategories()
    {
        if (!OpenWindow()) return;
        menu.SelectNav(1);
    }

    public void OpenCalendar()
    {
        if (!OpenWindow()) return;
        menu.ShowCalendar();
    }

    public void OpenProfile()
    {
        if (!OpenWindow()) return;
        menu.ShowProfile();
    }

    private bool OpenWindow()
    {
        if (menu == null || contentWindow == null) return false;
        contentWindow.gameObject.SetActive(true);
        var group = contentWindow.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 1f;
        return true;
    }
}
