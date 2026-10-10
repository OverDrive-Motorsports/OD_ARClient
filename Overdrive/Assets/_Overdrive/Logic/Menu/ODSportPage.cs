using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Three mock presentation states, ready for a later backend-driven mode.</summary>
public sealed class ODSportPage : MonoBehaviour
{
    public enum Mode { OffWeekend, Weekend, LiveSession }
    public OverdriveMainMenu menu;
    public Mode currentMode = Mode.Weekend;
    public GameObject[] modePanels;
    public TextMeshProUGUI heading;
    public TextMeshProUGUI subtitle;
    public TextMeshProUGUI status;
    public TextMeshProUGUI demoLabel;
    public RoundedImage statusSurface;
    public ODHomepageSurface background;
    public Button demoButton;
    public Button backButton;
    public string mockGrandPrix = "GP de Singapour";
    public string mockSession = "Course";
    private bool _bound;

    private void OnEnable()
    {
        Initialize();
        SetMode(currentMode);
    }

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        demoButton?.onClick.AddListener(NextMode);
        backButton?.onClick.AddListener(BackToCategories);
    }

    public void NextMode() => SetMode((Mode)(((int)currentMode + 1) % 3));

    public void BackToCategories()
    {
        if (menu != null) menu.ShowCategories();
    }

    public void SetMode(Mode mode)
    {
        currentMode = (Mode)Mathf.Clamp((int)mode, 0, 2);
        int index = (int)currentMode;
        if (modePanels != null)
            for (int i = 0; i < modePanels.Length; i++)
                if (modePanels[i] != null) modePanels[i].SetActive(i == index);
        string[] modes = { "Hors week-end", "Week-end en cours", "Live - " + mockSession };
        if (heading != null) heading.text = index == 0 ? "F1" : index == 1 ? mockGrandPrix : mockSession;
        if (subtitle != null) subtitle.text = "Formule 1 · " + mockGrandPrix;
        if (status != null) status.text = modes[index];
        if (demoLabel != null) demoLabel.text = "Mode démo · " + (index == 0 ? "Hors week-end" : index == 1 ? "Week-end" : "Live");
        Color accent = index == 0 ? new Color(.57f, .66f, .84f) : index == 1 ? new Color(1f, .73f, .30f) : new Color(1f, .34f, .28f);
        if (statusSurface != null) statusSurface.color = new Color(accent.r, accent.g, accent.b, .20f);
        if (background != null)
        {
            background.topLeft = index == 0 ? new Color(.025f, .04f, .075f, .94f) : index == 1 ? new Color(.06f, .026f, .016f, .94f) : new Color(.09f, .015f, .023f, .94f);
            background.bottomRight = new Color(.01f, .015f, .028f, .93f);
            background.SetVerticesDirty();
        }
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        demoButton?.onClick.RemoveListener(NextMode);
        backButton?.onClick.RemoveListener(BackToCategories);
    }
}
