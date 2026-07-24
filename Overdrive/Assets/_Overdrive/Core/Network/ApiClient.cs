/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ApiClient - Generic coroutine-based GET against the gateway (see ApiConfig
 ## for base URL/token). Deserializes with Newtonsoft (com.unity.nuget.newtonsoft-json)
 ## rather than JsonUtility, since gateway responses can be root-level JSON
 ## arrays and contain nullable fields, both unsupported by JsonUtility.
 ##
 ## Usage (from a MonoBehaviour, since coroutines need a runner):
 ##   StartCoroutine(ApiClient.Get<HealthStatusDTO>("/health", data => ..., err => ...));
 ##
 */

using System;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;

public static class ApiClient
{
    // Generic GET call: fetches a path and deserializes the JSON response into T.
    public static IEnumerator Get<T>(string path, Action<T> onSuccess, Action<DataError> onError)
    {
        string url = ApiConfig.BaseUrl.TrimEnd('/') + path;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(ApiConfig.AuthToken))
                request.SetRequestHeader("Authorization", "Bearer " + ApiConfig.AuthToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(new DataError(DataErrorKind.Network, "ApiClient",
                    $"GET {url} failed: {request.error} (HTTP {request.responseCode})"));
                yield break;
            }

            T data;
            try
            {
                data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
            }
            catch (JsonException e)
            {
                onError?.Invoke(new DataError(DataErrorKind.Deserialize, "ApiClient",
                    $"GET {url} returned unparseable JSON: {e.Message}"));
                yield break;
            }

            onSuccess?.Invoke(data);
        }
    }
}
