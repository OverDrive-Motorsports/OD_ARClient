using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Saved replay catalogue. Entries are mock records until the backend is connected.</summary>
public sealed class ODReplayCatalog : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string grandPrix;
        public string session = "Course";
        public string season = "2024";
        public Button openButton;
    }

    public OverdriveMainMenu menu;
    public ODSportPage sport;
    public Button backButton;
    public ScrollRect scroll;
    public Entry[] entries = Array.Empty<Entry>();
    private bool _bound;
    private UnityAction[] _openActions;

    private void OnEnable()
    {
        Initialize();
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    public void Initialize()
    {
        if (_bound) return;
        _bound = true;
        _openActions = new UnityAction[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            int index = i;
            _openActions[i] = () => OpenReplay(index);
            entries[i].openButton?.onClick.AddListener(_openActions[i]);
        }
        backButton?.onClick.AddListener(BackToSport);
    }

    public void OpenReplay(int index)
    {
        if (index < 0 || index >= entries.Length || menu == null || sport == null) return;
        sport.SelectReplay(entries[index]);
        menu.ShowSport();
    }

    public void BackToSport()
    {
        if (menu != null) menu.ShowSport();
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        for (int i = 0; i < entries.Length; i++)
            entries[i].openButton?.onClick.RemoveListener(_openActions[i]);
        backButton?.onClick.RemoveListener(BackToSport);
    }
}
