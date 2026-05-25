using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OverdriveMainMenu : MonoBehaviour
{
    [Header("Nav Buttons")]
    public Button racesButton;
    public Button rankingsButton;
    public Button profileButton;
    public Button settingsButton;

    [Header("Category Dropdown")]
    public TMP_Dropdown categoryDropdown;

    [Header("Content Grid")]
    public ContentGridView contentGrid;

    private void Start()
    {
        racesButton?.onClick.AddListener(() => OnNavSelected("Races"));
        rankingsButton?.onClick.AddListener(() => OnNavSelected("Rankings"));
        profileButton?.onClick.AddListener(() => OnNavSelected("Profile"));
        settingsButton?.onClick.AddListener(() => OnNavSelected("Settings"));

        categoryDropdown?.onValueChanged.AddListener(OnCategoryChanged);
    }

    private void OnNavSelected(string section)
    {
        // TODO: switch content based on section
        Debug.Log($"Nav: {section}");
    }

    private void OnCategoryChanged(int index)
    {
        // TODO: filter grid by category
        Debug.Log($"Category changed to index {index}");
    }
}
