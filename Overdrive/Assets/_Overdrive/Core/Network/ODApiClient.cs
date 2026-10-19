/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODApiClient - Thin async HTTP client for the OverDrive gateway: adds the
 ## Bearer token, applies the timeout, deserializes JSON into DTOs and turns
 ## every failure into an ODApiException. Knows nothing about UI or Logic.
 ##
 */

using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public enum ODApiErrorKind
{
    /// <summary>No response at all: backend down, wrong URL, no network, timeout.</summary>
    Unreachable,
    /// <summary>401 — missing or wrong gateway token.</summary>
    Unauthorized,
    /// <summary>Any other non-2xx status (404, 429, 502...).</summary>
    HttpError,
    /// <summary>The body couldn't be read as the expected DTO.</summary>
    InvalidResponse
}

public class ODApiException : Exception
{
    public ODApiErrorKind Kind { get; }
    public long StatusCode { get; }
    public string Url { get; }

    public ODApiException(ODApiErrorKind kind, string url, long statusCode, string message, Exception inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Url = url;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Every call runs on Unity's main thread (UnitySynchronizationContext), so
/// callers can touch scene objects right after an await.
/// </summary>
public static class ODApiClient
{
    static readonly JsonSerializerSettings Json = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    };

    /// <summary>GET {BaseUrl}{path} and deserialize the body. A 404 / empty body returns default(T).</summary>
    public static async Task<T> Get<T>(string path)
    {
        string body = await Send(UnityWebRequest.kHttpVerbGET, path, null, allowNotFound: true);
        return Parse<T>(path, body);
    }

    /// <summary>POST a JSON body (or none) and deserialize the response.</summary>
    public static async Task<T> Post<T>(string path, object payload = null)
    {
        string json = payload != null ? JsonConvert.SerializeObject(payload, Json) : null;
        string body = await Send(UnityWebRequest.kHttpVerbPOST, path, json, allowNotFound: false);
        return Parse<T>(path, body);
    }

    /// <summary>Gateway liveness — /health is open (no token). Never throws.</summary>
    public static async Task<bool> IsReachable()
    {
        try
        {
            await Send(UnityWebRequest.kHttpVerbGET, "/health", null, allowNotFound: false);
            return true;
        }
        catch (ODApiException)
        {
            return false;
        }
    }

    static async Task<string> Send(string method, string path, string jsonBody, bool allowNotFound)
    {
        var config = ODApiConfig.Instance;
        string url = config.BaseUrl + path;

        using var request = new UnityWebRequest(url, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(1, config.timeoutSeconds);
        request.SetRequestHeader("Accept", "application/json");
        if (!string.IsNullOrEmpty(config.authToken))
            request.SetRequestHeader("Authorization", "Bearer " + config.authToken);
        if (jsonBody != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.SetRequestHeader("Content-Type", "application/json");
        }

        try
        {
            await request.SendWebRequest();
        }
        catch (Exception e) when (!(e is ODApiException))
        {
            // Awaiting a failed UnityWebRequest doesn't throw on its own; this
            // only catches unexpected errors (e.g. "Insecure connection not allowed").
            throw new ODApiException(ODApiErrorKind.Unreachable, url, 0, e.Message, e);
        }

        long status = request.responseCode;
        switch (request.result)
        {
            case UnityWebRequest.Result.Success:
                return request.downloadHandler.text;

            case UnityWebRequest.Result.ProtocolError:
                if (allowNotFound && status == 404) return null;
                throw new ODApiException(status == 401 ? ODApiErrorKind.Unauthorized : ODApiErrorKind.HttpError,
                    url, status, $"HTTP {status} — {request.downloadHandler.text}");

            default:
                throw new ODApiException(ODApiErrorKind.Unreachable, url, status, request.error);
        }
    }

    static T Parse<T>(string url, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null") return default;
        try
        {
            return JsonConvert.DeserializeObject<T>(body, Json);
        }
        catch (Exception e)
        {
            throw new ODApiException(ODApiErrorKind.InvalidResponse, url, 200, $"Invalid JSON for {typeof(T).Name}: {e.Message}", e);
        }
    }
}
