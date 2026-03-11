using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

// Les classes pour lire le JSON
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
                // Axe Z de l'API = altitude. Donc X, Y deviennent X, Z.
                // On met Y à 0 par défaut, on le modifiera dans Update()
                Vector3 targetPos = new Vector3(loc.x * scaleFactor, 0f, loc.y * scaleFactor);
                positionBuffer.Enqueue(targetPos);
            }
        }
    }

    void Update()
    {
        // 1. Prendre la prochaine cible si on a fini la précédente
        if (!hasTarget && positionBuffer.Count > 0)
        {
            currentTarget = positionBuffer.Dequeue();
            
            // On force la cible à avoir la même hauteur locale
            currentTarget.y = transform.localPosition.y;
            
            hasTarget = true;
        }

        // 2. Animer la voiture vers la cible
        if (hasTarget)
        {
            float step = animationSpeed * Time.deltaTime;
            
            // Déplacement (On avance normalement vers la cible)
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, currentTarget, step);

            // Calcul de direction locale
            Vector3 localDirection = currentTarget - transform.localPosition;
            localDirection.y = 0f; // On s'assure qu'on regarde à plat

            if (localDirection != Vector3.zero)
            {
                // Orienter la voiture vers sa direction de déplacement (plan XZ)
                Quaternion targetRotation = Quaternion.LookRotation(localDirection);

                // Conserver l'inclinaison actuelle (X et Z) pour éviter de pencher
                Vector3 currentEuler = transform.localEulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;

                Quaternion finalRotation = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);

                // Rotation fluide vers l'orientation cible
                transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, Time.deltaTime * 10f);
            }

            // On est arrivé au point ?
            if (Vector3.Distance(transform.localPosition, currentTarget) < 0.1f)
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
