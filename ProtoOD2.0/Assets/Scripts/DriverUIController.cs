using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class DriverApiEntry
{
    public string broadcast_name;
    public string country_code;
    public int driver_number;
    public string first_name;
    public string full_name;
    public string headshot_url;
    public string last_name;
    public int meeting_key;
    public string name_acronym;
    public int session_key;
    public string team_colour;
    public string team_name;
}

[Serializable]
public class DriversApiResponse
{
    public int count;
    public List<DriverApiEntry> data;
    public string dataset;
    // metadata ignoré
}

public class DriverUIController : MonoBehaviour
{
    [Header("Text fields")]
    public TMP_Text nameText;
    public TMP_Text teamNameText;
    public TMP_Text numberText;

    [Header("Dropdown")]
    public TMP_Dropdown driverDropdown;

    [Header("Image")]
    public DriverImageLoader imageLoader;

    [Header("API")]
    public string apiUrl = "http://172.20.10.5:8080/api/v1/race/drivers";

    private DriversApiResponse allData;

    void Start()
    {
        StartCoroutine(LoadDriversFromApi());
    }

    IEnumerator LoadDriversFromApi()
    {
        Debug.Log("[DriverUI] Call " + apiUrl);

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            Debug.Log("[DriverUI] result = " + request.result + " code = " + request.responseCode);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[DriverUI] Erreur API drivers: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("[DriverUI] raw json = " + json.Substring(0, Mathf.Min(200, json.Length)) + "...");

            // Désérialisation
            allData = JsonUtility.FromJson<DriversApiResponse>(json);

            if (allData == null || allData.data == null || allData.data.Count == 0)
            {
                Debug.LogError("[DriverUI] Pas de données drivers reçues ou parse raté.");
                yield break;
            }

            Debug.Log("[DriverUI] drivers count = " + allData.count);

            SetupDropdown();
            OnDriverSelected(0);
            driverDropdown.onValueChanged.AddListener(OnDriverSelected);
        }
    }

    void SetupDropdown()
    {
        driverDropdown.ClearOptions();

        foreach (var entry in allData.data)
        {
            string optionLabel = entry.full_name;
            driverDropdown.options.Add(new TMP_Dropdown.OptionData(optionLabel));
        }

        driverDropdown.RefreshShownValue();
    }

    void OnDriverSelected(int index)
    {
        if (allData == null || allData.data == null || allData.data.Count == 0)
            return;

        DriverApiEntry entry = allData.data[index];

        string fullName = entry.full_name;
        string team = entry.team_name;
        int number = entry.driver_number;
        string headshotUrl = entry.headshot_url;

        nameText.text = fullName;
        teamNameText.text = team;
        numberText.text = number.ToString();

        if (imageLoader != null && !string.IsNullOrEmpty(headshotUrl))
        {
            imageLoader.SetHeadshot(headshotUrl);
        }
    }

    public void NextDriver()
    {
        if (allData == null || allData.data == null || allData.data.Count == 0)
            return;

        int index = driverDropdown.value;
        index++;

        if (index >= allData.data.Count)
            index = 0;

        driverDropdown.value = index;
        OnDriverSelected(index);
    }

    public void PreviousDriver()
    {
        if (allData == null || allData.data == null || allData.data.Count == 0)
            return;

        int index = driverDropdown.value;
        index--;

        if (index < 0)
            index = allData.data.Count - 1;

        driverDropdown.value = index;
        OnDriverSelected(index);
    }
}
