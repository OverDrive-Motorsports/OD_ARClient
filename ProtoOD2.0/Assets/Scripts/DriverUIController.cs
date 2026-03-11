using UnityEngine;
using TMPro;

public class DriverUIController : MonoBehaviour
{
    [Header("Text fields")]
    public TMP_Text nameText;
    public TMP_Text teamNameText;
    public TMP_Text numberText;

    [Header("Dropdown")]
    public TMP_Dropdown driverDropdown;

    [Header("Image")]
    public DriverImageLoader imageLoader;   // Chargé de télécharger et d'afficher la photo du pilote

    private RootDriverData allData;

    void Start()
    {
        LoadJson();
        SetupDropdown();
        OnDriverSelected(0);
        driverDropdown.onValueChanged.AddListener(OnDriverSelected);
    }

    void LoadJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Driver_Datas");
        allData = JsonUtility.FromJson<RootDriverData>(jsonFile.text);
    }

    void SetupDropdown()
    {
        driverDropdown.ClearOptions();

        foreach (var entry in allData.data)
        {
            string optionLabel = entry.driver.full_name;
            driverDropdown.options.Add(new TMP_Dropdown.OptionData(optionLabel));
        }

        driverDropdown.RefreshShownValue();
    }

    void OnDriverSelected(int index)
    {
        DriverEntry entry = allData.data[index];

        string fullName = entry.driver.full_name;
        string team = entry.driver.team_name;
        int number = entry.driver.driver_number;
        string headshotUrl = entry.driver.headshot_url;  // URL de la photo du pilote fournie par les données

        nameText.text = fullName;
        teamNameText.text = team;
        numberText.text = number.ToString();

        if (imageLoader != null && !string.IsNullOrEmpty(headshotUrl))
        {
            imageLoader.SetHeadshot(headshotUrl);
        }
    }
}
