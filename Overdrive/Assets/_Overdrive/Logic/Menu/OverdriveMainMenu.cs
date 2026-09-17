using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles nav-button selection, content-panel switching,
/// and category cycling on the Overdrive main menu.
/// Auto-wires all references at Start() — no Inspector assignment needed.
/// </summary>
public class OverdriveMainMenu : MonoBehaviour
{
    [Header("Nav Buttons (auto-found if empty)")]
    public Button racesButton;
    public Button rankingsButton;
    public Button profileButton;
    public Button settingsButton;
    [Tooltip("Navigation button events are owned by a separate navigation window.")]
    public bool externalNavigation;

    [Header("Category (auto-found if empty)")]
    public Button categoryButton;
    public TextMeshProUGUI categoryLabel;

    [Header("Nav Style")]
    public Color activeNavColor    = new Color(0.72f, 0.79f, 0.86f, 0.32f);
    public Color inactiveNavColor  = Color.clear;
    public Color activeTextColor   = Color.white;
    public Color inactiveTextColor = new Color(1f, 1f, 1f, 0.80f);

    // ── private ──────────────────────────────────────────────────────────────
    private Button[] _navButtons;
    private GameObject[] _panels;
    private int _currentCat = 0;

    private static readonly string[] Categories = { "F1  ▾", "MotoGP  ▾", "IndyCar  ▾", "F2  ▾" };

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_navButtons != null) return;
        AutoFindReferences();

        _navButtons = new[] { racesButton, rankingsButton, profileButton, settingsButton };

        // Build content panels (Races reuses existing grid, others are placeholders)
        _panels = new[]
        {
            FindChildGO("Body/ContentArea/GridScrollView"),
            GetOrCreatePanel("Rankings"),
            GetOrCreatePanel("Profile"),
            GetOrCreatePanel("Settings")
        };

        // Wire nav clicks
        if (!externalNavigation)
        {
            racesButton?   .onClick.AddListener(() => SelectNav(0));
            rankingsButton?.onClick.AddListener(() => SelectNav(1));
            profileButton? .onClick.AddListener(() => SelectNav(2));
            settingsButton?.onClick.AddListener(() => SelectNav(3));
        }

        // Wire category click
        categoryButton?.onClick.AddListener(CycleCategory);

        SelectNav(0);
    }

    // ── Nav ───────────────────────────────────────────────────────────────────
    public void SelectNav(int index)
    {
        Initialize();
        if (index < 0 || index >= _navButtons.Length) return;
        for (int i = 0; i < _navButtons.Length; i++)
        {
            if (_navButtons[i] == null) continue;

            bool active = (i == index);

            // The Button tint MULTIPLIES the graphic colour, so the graphic must
            // stay white and all states live in the ColorBlock — otherwise a
            // transparent base makes hover invisible.
            var img = _navButtons[i].GetComponent<Image>();
            if (img) img.color = Color.white;

            var cb = _navButtons[i].colors;
            cb.normalColor      = active ? activeNavColor : Color.clear;
            cb.highlightedColor = new Color(0.78f, 0.84f, 0.90f, 0.38f);
            cb.pressedColor     = new Color(0.84f, 0.90f, 0.96f, 0.48f);
            cb.selectedColor    = cb.normalColor;
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            _navButtons[i].colors = cb;

            var lbl = _navButtons[i].transform.Find("Label")
                                    ?.GetComponent<TextMeshProUGUI>();
            if (lbl) lbl.color = active ? activeTextColor : inactiveTextColor;
            var icon = _navButtons[i].GetComponentInChildren<ODNavigationIcon>(true);
            if (icon) icon.color = active ? activeTextColor : inactiveTextColor;
        }

        for (int i = 0; i < _panels.Length; i++)
            if (_panels[i] != null)
                _panels[i].SetActive(i == index);

        // Category selector only makes sense on the Races tab
        if (categoryLabel != null)
            categoryLabel.gameObject.SetActive(index == 0);
    }

    // ── Category ──────────────────────────────────────────────────────────────
    private void CycleCategory()
    {
        _currentCat = (_currentCat + 1) % Categories.Length;
        if (categoryLabel) categoryLabel.text = Categories[_currentCat];
    }

    // ── Auto-wire ─────────────────────────────────────────────────────────────
    private void AutoFindReferences()
    {
        // Nav buttons
        // Explicit null checks — never ?? with Unity objects (fake-null)
        if (racesButton    == null) racesButton    = FindNavButton("Nav_Races");
        if (rankingsButton == null) rankingsButton = FindNavButton("Nav_Rankings");
        if (profileButton  == null) profileButton  = FindNavButton("Nav_Profile");
        if (settingsButton == null) settingsButton = FindNavButton("Nav_Settings");

        // Category label
        var catT = FindChildTransform("Body/ContentArea/CategoryLabel");
        if (catT != null)
        {
            if (categoryLabel == null) categoryLabel = catT.GetComponent<TextMeshProUGUI>();
            if (categoryButton == null)
            {
                categoryButton = catT.GetComponent<Button>();
                if (categoryButton == null)
                {
                    categoryButton = catT.gameObject.AddComponent<Button>();
                    categoryButton.targetGraphic = catT.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Button FindNavButton(string name)
    {
        var button = FindButton("BottomNav/" + name);
        return button != null ? button : FindButton("Body/Sidebar/" + name);
    }

    private Button FindButton(string path)
    {
        var t = FindChildTransform(path);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private GameObject FindChildGO(string path)
    {
        var t = FindChildTransform(path);
        return t != null ? t.gameObject : null;
    }

    private Transform FindChildTransform(string path)
    {
        Transform t = null;
        if (transform.parent != null) t = transform.parent.Find(path);
        if (t == null) t = transform.Find(path);
        return t;
    }

    private GameObject GetOrCreatePanel(string label)
    {
        var existing = FindChildGO("Body/ContentArea/" + label + "Panel");
        if (existing != null) return existing;

        var contentArea = FindChildTransform("Body/ContentArea");
        if (contentArea == null) return null;

        var go = new GameObject(label + "Panel");
        go.transform.SetParent(contentArea, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.AddComponent<Image>().color = UITheme.Instance != null
            ? UITheme.Instance.subtleColor : new Color(0.20f, 0.25f, 0.32f, 0.5f);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label + "\n<size=60%>Coming soon</size>";
        tmp.fontSize = 32f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        go.SetActive(false);
        return go;
    }
}
