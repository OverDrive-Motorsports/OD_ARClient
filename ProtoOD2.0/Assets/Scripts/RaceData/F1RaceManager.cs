using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F1RaceManager : MonoBehaviour
{
    [Header("API")]
    public string sessionApiUrl = "http://172.20.10.5:8080/api/v1/sessions/657f962f-56c3-47b9-864d-0439db162870";
    
    [Header("Spawning")]
    public GameObject carPrefab;
    public Transform trackAnchor;

    private const int RUSSELL_NUMBER = 63;

    void Start()
    {
        SpawnCarForDriver(RUSSELL_NUMBER);
    }

    void SpawnCarForDriver(int driverNum)
    {
        GameObject newCar = Instantiate(carPrefab, trackAnchor);
        newCar.name = "F1_Car_" + driverNum;

        F1CarAnimator animator = newCar.GetComponent<F1CarAnimator>();

        if (animator != null)
        {
            animator.driverNumber = driverNum;
            animator.baseApiUrl = sessionApiUrl;
            animator.StartRace();
        }
        else
        {
            Debug.LogError("F1CarAnimator manquant sur le prefab de voiture !");
        }
    }
}
