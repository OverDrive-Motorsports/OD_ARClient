using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Four presentation states, ready for backend events and replay media.</summary>
public sealed class ODSportPage : MonoBehaviour
{
    public enum Mode { OffWeekend = 0, Weekend = 1, LiveSession = 2, Replay = 3 }
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
    public Button replayButton;
    public VideoPlayerController television;
    public CircuitPreview circuitPreview;
    public Button[] circuitButtons;
    public string mockGrandPrix = "GP de Singapour";
    public string mockSession = "Course";
    [Header("Selected replay (catalogue demo)")]
    public string replayId;
    public string replayGrandPrix = "GP de Singapour";
    public string replaySession = "Course";
    public string replaySeason = "2024";
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
        replayButton?.onClick.AddListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.AddListener(ToggleCircuits);
        television?.Initialize();
    }

    public void NextMode() => SetMode((Mode)(((int)currentMode + 1) % 4));

    public void OpenReplayCatalog()
    {
        if (menu != null) menu.ShowReplayCatalog();
    }

    public void SelectReplay(ODReplayCatalog.Entry entry)
    {
        if (entry == null) return;
        replayId = entry.id;
        replayGrandPrix = entry.grandPrix;
        replaySession = entry.session;
        replaySeason = entry.season;
        SetMode(Mode.Replay);
    }

    public void BackToCategories()
    {
        if (menu != null) menu.ShowCategories();
    }

    public void ToggleCircuits()
    {
        if (!isActiveAndEnabled || circuitPreview == null) return;
        if (circuitPreview.gameObject.activeSelf) circuitPreview.Hide();
        else circuitPreview.ShowBeside(menu != null ? (RectTransform)menu.transform : (RectTransform)transform, true);
    }

    private void OnDisable() => circuitPreview?.Hide();

    public void SetMode(Mode mode)
    {
        if (currentMode != mode) circuitPreview?.Hide();
        currentMode = (Mode)Mathf.Clamp((int)mode, 0, 3);
        int index = (int)currentMode;
        if (modePanels != null)
            for (int i = 0; i < modePanels.Length; i++)
                if (modePanels[i] != null) modePanels[i].SetActive(i == index);
        bool replay = currentMode == Mode.Replay;
        string[] modes = { "Hors week-end", "Week-end en cours", "Live - " + mockSession, "Replay" };
        if (heading != null) heading.text = replay ? replayGrandPrix : index == 0 ? "F1" : index == 1 ? mockGrandPrix : mockSession;
        if (subtitle != null) subtitle.text = replay
            ? "Formule 1 · " + replaySession + " · Saison " + replaySeason
            : "Formule 1 · " + mockGrandPrix;
        if (status != null) status.text = modes[index];
        if (demoLabel != null) demoLabel.text = "Mode démo · " + (replay ? "Replay" : index == 0 ? "Hors week-end" : index == 1 ? "Week-end" : "Live");
        Color accent = replay ? new Color(.46f, .77f, 1f) : index == 0 ? new Color(.57f, .66f, .84f) : index == 1 ? new Color(1f, .73f, .30f) : new Color(1f, .34f, .28f);
        if (statusSurface != null) statusSurface.color = new Color(accent.r, accent.g, accent.b, .20f);
        if (background != null)
        {
            background.topLeft = replay ? new Color(.018f, .04f, .075f, .94f) : index == 0 ? new Color(.025f, .04f, .075f, .94f) : index == 1 ? new Color(.06f, .026f, .016f, .94f) : new Color(.09f, .015f, .023f, .94f);
            background.bottomRight = new Color(.01f, .015f, .028f, .93f);
            background.SetVerticesDirty();
        }
        if (television != null) television.Configure(currentMode,
            replay ? "replay:" + replayId + ":" + replayGrandPrix : "live:" + index + ":" + mockGrandPrix + ":" + mockSession,
            replay ? replayGrandPrix : mockGrandPrix);
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        demoButton?.onClick.RemoveListener(NextMode);
        backButton?.onClick.RemoveListener(BackToCategories);
        replayButton?.onClick.RemoveListener(OpenReplayCatalog);
        if (circuitButtons != null)
            foreach (var button in circuitButtons) button?.onClick.RemoveListener(ToggleCircuits);
    }
}
