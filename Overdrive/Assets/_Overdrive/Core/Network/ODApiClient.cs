/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODApiClient - Thin async HTTP client for the OverDrive gateway: adds the
 ## Bearer token, applies the timeout, deserializes JSON into DTOs (off the
 ## main thread), streams large bodies to disk and turns every failure into
 ## an ODApiException. Knows nothing about UI or Logic.
 ##
 */

using System;
using System.Threading;
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
    /// <summary>Backend error code ("UPSTREAM_UNAVAILABLE", "SESSION_NOT_FOUND"...), null when the body had none.</summary>
    public string Code { get; }

    public ODApiException(ODApiErrorKind kind, string url, long statusCode, string message, Exception inner = null, string code = null)
        : base(message, inner)
    {
        Kind = kind;
        Url = url;
        StatusCode = statusCode;
        Code = code;
    }
}

/// <summary>Error body written by every backend service (shared/apierror).</summary>
public class ApiErrorBodyDto
{
    public ApiErrorDto error;
}

public class ApiErrorDto
{
    public string code;
    public string message;
}

/// <summary>
/// Calls start and finish on Unity's main thread (UnitySynchronizationContext),
/// so callers can touch scene objects right after an await.
/// </summary>
public static class ODApiClient
{
    public static readonly JsonSerializerSettings Json = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    };

    /// <summary>GET {BaseUrl}{path} and deserialize the body. A 404 / empty body returns default(T).</summary>
    public static async Task<T> Get<T>(string path)
    {
        using var request = CreateRequest(path, ODApiConfig.Instance.timeoutSeconds);
        request.downloadHandler = new DownloadHandlerBuffer();
        await Send(request);
        if (ThrowIfFailed(request, allowNotFound: true)) return default;

        string body = request.downloadHandler.text;
        if (string.IsNullOrWhiteSpace(body)) return default;
        try
        {
            // Off the main thread: a few hundred kB of JSON would otherwise cost frames on the headset.
            return await Task.Run(() => JsonConvert.DeserializeObject<T>(body, Json));
        }
        catch (Exception e)
        {
            throw new ODApiException(ODApiErrorKind.InvalidResponse, request.url, 200, $"Invalid JSON for {typeof(T).Name}: {e.Message}", e);
        }
    }

    /// <summary>
    /// GET {BaseUrl}{path} straight into <paramref name="filePath"/>, never holding the body in memory.
    /// Reports the bytes received so far (the backend streams without Content-Length). Gives up when
    /// nothing arrives for ODApiConfig.downloadStallSeconds; cancelling aborts the request. Throws an
    /// ODApiException (or OperationCanceledException) on failure — the caller deletes the partial file.
    /// </summary>
    public static async Task Download(string path, string filePath, IProgress<ulong> progress, CancellationToken token)
    {
        var config = ODApiConfig.Instance;
        using var request = CreateRequest(path, 0);   // no overall timeout: a full race takes a while
        request.downloadHandler = new DownloadHandlerFile(filePath) { removeFileOnAbort = true };

        var operation = request.SendWebRequest();
        ulong received = 0;
        float lastByteAt = Time.realtimeSinceStartup;
        bool stalled = false;
        while (!operation.isDone)
        {
            if (token.IsCancellationRequested) { request.Abort(); break; }
            if (request.downloadedBytes != received)
            {
                received = request.downloadedBytes;
                lastByteAt = Time.realtimeSinceStartup;
                progress?.Report(received);
            }
            else if (Time.realtimeSinceStartup - lastByteAt > Mathf.Max(1, config.downloadStallSeconds))
            {
                stalled = true;
                request.Abort();
                break;
            }
            await Task.Yield();   // next frame (UnitySynchronizationContext), in Play mode and in the Editor
        }

        token.ThrowIfCancellationRequested();
        if (stalled)
            throw new ODApiException(ODApiErrorKind.Unreachable, request.url, 0, $"No data for {config.downloadStallSeconds}s");
        ThrowIfFailed(request, allowNotFound: false);
        progress?.Report(request.downloadedBytes);
    }

    static UnityWebRequest CreateRequest(string path, int timeoutSeconds)
    {
        var config = ODApiConfig.Instance;
        var request = new UnityWebRequest(config.BaseUrl + path, UnityWebRequest.kHttpVerbGET);
        request.timeout = Mathf.Max(0, timeoutSeconds);
        request.SetRequestHeader("Accept", "application/json");
        if (!string.IsNullOrEmpty(config.authToken))
            request.SetRequestHeader("Authorization", "Bearer " + config.authToken);
        return request;
    }

    static async Task Send(UnityWebRequest request)
    {
        try
        {
            await request.SendWebRequest();
        }
        catch (Exception e)
        {
            // Awaiting a failed UnityWebRequest doesn't throw on its own; this
            // only catches unexpected errors (e.g. "Insecure connection not allowed").
            throw new ODApiException(ODApiErrorKind.Unreachable, request.url, 0, e.Message, e);
        }
    }

    /// <summary>Throws for any failed request; returns true for a tolerated 404.</summary>
    static bool ThrowIfFailed(UnityWebRequest request, bool allowNotFound)
    {
        long status = request.responseCode;
        switch (request.result)
        {
            case UnityWebRequest.Result.Success:
                return false;

            case UnityWebRequest.Result.ProtocolError:
                if (allowNotFound && status == 404) return true;
                var error = ReadError(request);
                throw new ODApiException(status == 401 ? ODApiErrorKind.Unauthorized : ODApiErrorKind.HttpError,
                    request.url, status, $"HTTP {status} — {error?.message ?? "no details"}", code: error?.code);

            default:
                throw new ODApiException(ODApiErrorKind.Unreachable, request.url, status, request.error);
        }
    }

    /// <summary>The backend's { "error": { code, message } } body, when the handler kept it in memory.</summary>
    static ApiErrorDto ReadError(UnityWebRequest request)
    {
        if (!(request.downloadHandler is DownloadHandlerBuffer buffer)) return null;
        try
        {
            return JsonConvert.DeserializeObject<ApiErrorBodyDto>(buffer.text, Json)?.error;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
