/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODApiConfig - Backend connection settings (gateway URL, auth token,
 ## championship code). Loaded from Resources/ODApiConfig when the asset
 ## exists, otherwise falls back to local-development defaults.
 ##
 */

using UnityEngine;

/// <summary>
/// The asset lives in Core/Resources/ODApiConfig.asset and is git-ignored
/// because it holds the gateway token. Create it from
/// Assets > Create > Overdrive > API Config when it's missing.
/// </summary>
[CreateAssetMenu(fileName = "ODApiConfig", menuName = "Overdrive/API Config")]
public class ODApiConfig : ScriptableObject
{
    [Tooltip("Gateway URL used in the Editor (the backend runs on this machine).")]
    public string editorBaseUrl = "http://127.0.0.1:3000";

    [Tooltip("Gateway URL used on the headset — the LAN IP of the machine running the backend.")]
    public string deviceBaseUrl = "http://192.168.1.10:3000";

    [Tooltip("GATEWAY_AUTH_TOKEN from the backend's gateway/.env. Mandatory on every /v1 route.")]
    public string authToken = "";

    [Tooltip("Championship code exposed by championship-service (OpenF1 ingestion uses \"f1\").")]
    public string championshipCode = "f1";

    [Tooltip("Per-request timeout, in seconds.")]
    public int timeoutSeconds = 8;

    [Tooltip("Polling period of the live session, in seconds (the backend has no WebSocket yet).")]
    public float livePollSeconds = 5f;

    public string BaseUrl => (Application.isEditor ? editorBaseUrl : deviceBaseUrl).TrimEnd('/');

    static ODApiConfig _instance;

    public static ODApiConfig Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<ODApiConfig>("ODApiConfig");
            if (_instance == null) _instance = CreateInstance<ODApiConfig>();
            return _instance;
        }
    }
}
