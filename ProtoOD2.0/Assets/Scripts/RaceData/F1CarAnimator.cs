using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[Serializable]
public class LapLocationData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class LapResponseWrapper
{
    public LapLocationData[] data;
}

public class F1CarAnimator : MonoBehaviour
{
    [Header("Paramètres")]
    public string baseApiUrl;
    public int driverNumber;
    public float scaleFactor = 0.01f;
    public float animationSpeed = 50f;

    private int currentLap = 1;
    private bool isFetchingLap = false;
    private bool raceFinished = false;

    private Queue<Vector3> positionBuffer = new Queue<Vector3>();
    private Vector3 currentTarget;
    private bool hasTarget = false;

    public void StartRace()
    {
        currentLap = 1;
        raceFinished = false;
        positionBuffer.Clear();
        StartCoroutine(FetchLap(currentLap));
    }

    IEnumerator FetchLap(int lapNumber)
    {
        if (isFetchingLap || raceFinished) yield break;

        isFetchingLap = true;
        string url = $"{baseApiUrl}/drivers/{driverNumber}/laps/{lapNumber}/location";
        Debug.Log($"[Car {driverNumber}] Fetch lap {lapNumber} -> {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Car {driverNumber}] HTTP error: {webRequest.error}");
                isFetchingLap = false;
                yield break;
            }

            if (webRequest.responseCode == 404)
            {
                Debug.Log($"[Car {driverNumber}] No more laps after lap {lapNumber - 1}");
                raceFinished = true;
                isFetchingLap = false;
                yield break;
            }

            string rawJson = webRequest.downloadHandler.text.Trim();

            if (rawJson.StartsWith("["))
                rawJson = "{\"data\":" + rawJson + "}";

            LapResponseWrapper response = JsonUtility.FromJson<LapResponseWrapper>(rawJson);

            if (response == null || response.data == null || response.data.Length == 0)
            {
                Debug.Log($"[Car {driverNumber}] Empty lap {lapNumber}, stopping.");
                raceFinished = true;
                isFetchingLap = false;
                yield break;
            }

            foreach (var point in response.data)
            {
                Vector3 pos = new Vector3(
                    point.x * scaleFactor,
                    0f,
                    point.y * scaleFactor
                );
                positionBuffer.Enqueue(pos);
            }

            Debug.Log($"[Car {driverNumber}] Lap {lapNumber} loaded with {response.data.Length} points");
        }

        isFetchingLap = false;
    }

    void Update()
    {
        if (!hasTarget && positionBuffer.Count > 0)
        {
            currentTarget = positionBuffer.Dequeue();
            currentTarget.y = transform.localPosition.y;
            hasTarget = true;
        }

        if (hasTarget)
        {
            float step = animationSpeed * Time.deltaTime;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, currentTarget, step);

            Vector3 direction = currentTarget - transform.localPosition;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * 10f);
            }

            if (Vector3.Distance(transform.localPosition, currentTarget) < 0.1f)
            {
                hasTarget = false;
            }
        }

        if (!raceFinished && !isFetchingLap && !hasTarget && positionBuffer.Count == 0)
        {
            currentLap++;
            StartCoroutine(FetchLap(currentLap));
        }
    }
}
