using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[Serializable]
public class RaceDriverInfo
{
    public int driver_number;
    public string team_colour; 
}

[Serializable]
public class DriversListResponse
{
    public List<RaceDriverInfo> data;
}

public class F1RaceManager : MonoBehaviour
{
    [Header("API (Liste des Pilotes)")]
    // URL pour obtenir les 20 pilotes de la course stockée actuellement (Bahreïn)
    public string driversUrl = "http://192.168.1.65:8080/api/v1/race/drivers";
    
    [Header("API (Pour les Voitures)")]
    // L'URL de base de la session de Bahreïn pour les voitures
    public string sessionApiUrl = "http://192.168.1.65:8080/api/v1/sessions/9472";
    
    [Header("Spawning")]
    public GameObject carPrefab;
    public Transform trackAnchor;

    void Start()
    {
        StartCoroutine(FetchDriversAndSpawnCars());
    }

    IEnumerator FetchDriversAndSpawnCars()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(driversUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                DriversListResponse response = JsonUtility.FromJson<DriversListResponse>(json);

                if (response != null && response.data != null)
                {
                    foreach (var driver in response.data)
                    {
                        SpawnCarForDriver(driver.driver_number);
                    }
                }
            }
            else
            {
                Debug.LogError("Erreur chargement pilotes : " + request.error);
            }
        }
    }

    void SpawnCarForDriver(int driverNum)
    {
        GameObject newCar = Instantiate(carPrefab, trackAnchor);
        newCar.name = "F1_Car_" + driverNum;

        F1CarAnimator animator = newCar.GetComponent<F1CarAnimator>();
        
        if (animator != null)
        {
            // On donne à la voiture son numéro ET l'URL exacte de la session
            animator.driverNumber = driverNum;
            animator.baseApiUrl = sessionApiUrl; 
            
            animator.StartRace(); 
        }
    }
}
