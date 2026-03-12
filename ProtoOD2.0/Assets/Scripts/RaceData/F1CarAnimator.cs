using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[Serializable]
public class LocationData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class LocationResponse
{
    public LocationData[] data; 
}

public class F1CarAnimator : MonoBehaviour
{
    [Header("Paramètres de la voiture")]
    public string sessionKey = "9472"; // Course Bahreïn 2024
    public int driverNumber = 1;       // 1 = Max Verstappen
    public string startTime = "2024-03-02T15:03:42.341"; // Début du tour 1
    
    [Header("Réglages visuels")]
    public float fetchInterval = 3f;   // Télécharge les données toutes les 3 sec
    public float scaleFactor = 0.01f;  // Échelle pour Unity
    public float animationSpeed = 50f; // Vitesse de déplacement

    private Queue<Vector3> positionBuffer = new Queue<Vector3>();
    private Vector3 currentTarget;
    private bool hasTarget = false;
    private string lastFetchTime;
    private Vector3 startPos;
    private float lerpTime = 0f;
    public float pointTravelTime = 0.25f;

    void Start()
    {
        lastFetchTime = startTime;
        StartCoroutine(FetchLocationLoop());
    }

    IEnumerator FetchLocationLoop()
    {
        while (true)
        {
            string nextTime = CalculateNextTime(lastFetchTime, fetchInterval);

            // On demande UNIQUEMENT les positions de ce pilote spécifique
            string url = $"https://api.openf1.org/v1/location?session_key={sessionKey}&driver_number={driverNumber}&date>{lastFetchTime}&date<={nextTime}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest(); 

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    ProcessApiData(webRequest.downloadHandler.text);
                    lastFetchTime = nextTime; 
                }
            }

            yield return new WaitForSeconds(fetchInterval);
        }
    }

    void ProcessApiData(string json)
    {
        string wrappedJson = "{\"data\":" + json + "}";
        LocationResponse response = JsonUtility.FromJson<LocationResponse>(wrappedJson);

        if (response != null && response.data != null)
        {
            foreach (var loc in response.data)
            {
                Vector3 targetPos = new Vector3(loc.x * scaleFactor, 0f, loc.y * scaleFactor);
                positionBuffer.Enqueue(targetPos);
            }
        }
    }

    void Update()
    {
        // 1. Si on n'a pas de cible en cours, on en prend une nouvelle
        if (!hasTarget && positionBuffer.Count > 0)
        {
            startPos = transform.localPosition;
            currentTarget = positionBuffer.Dequeue();

            // verrouille la hauteur
            currentTarget.y = startPos.y;

            lerpTime = 0f;
            hasTarget = true;
        }

        if (hasTarget)
        {
            // 2. On fait avancer t de 0 -> 1 sur pointTravelTime secondes
            lerpTime += Time.deltaTime / pointTravelTime;
            float t = Mathf.Clamp01(lerpTime);

            // 3. Interpolation de position (courbe lissée si tu veux avec SmoothStep)
            Vector3 newPos = Vector3.Lerp(startPos, currentTarget, Mathf.SmoothStep(0f, 1f, t));
            transform.localPosition = newPos;

            // 4. Rotation douce vers la direction de déplacement (à plat)
            Vector3 dir = currentTarget - startPos;
            dir.y = 0f;

            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                Vector3 currentEuler = transform.localEulerAngles;
                Vector3 targetEuler = targetRot.eulerAngles;

                Quaternion finalRot = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRot, Time.deltaTime * 10f);
            }

            // 5. Quand t atteint 1, on passe au point suivant
            if (t >= 1f)
            {
                hasTarget = false;
            }
        }
    }


    string CalculateNextTime(string current, float addSeconds)
    {
        DateTime dt = DateTime.Parse(current, System.Globalization.CultureInfo.InvariantCulture);
        dt = dt.AddSeconds(addSeconds);
        return dt.ToString("yyyy-MM-ddTHH:mm:ss.fff");
    }
}
