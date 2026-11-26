using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public enum ODApiErrorKind
{
    Unreachable,
    Unauthorized,
    HttpError,
    InvalidResponse
}

public class ODApiException : Exception
{
    public ODApiErrorKind Kind { get; }
    public long StatusCode { get; }
    public string Url { get; }
    public string Code { get; }

    public ODApiException(ODApiErrorKind kind, string url, long statusCode, string message, Exception inner = null, string code = null)
        : base(message, inner)
    {
        Kind = kind; Url = url; StatusCode = statusCode; Code = code;
    }
}

public class ApiErrorBodyDto { public ApiErrorDto error; }
public class ApiErrorDto { public string code; public string message; }

/// <summary>Authenticated gateway transport. JSON parses off-thread; downloads stream to disk. Health stays public.</summary>
public static class ODApiClient
{
    public static readonly JsonSerializerSettings Json = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    };

    /// <summary>A 404 / empty response returns default. Every protected call uses the current user's JWT.</summary>
    public static async Task<T> Get<T>(string path)
    {
        int version = ODAuthService.Version;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            string token = await TokenFor(path);
            EnsureSession(path, version);
            using var request = CreateRequest(path, ODApiConfig.Instance.timeoutSeconds, token);
            request.downloadHandler = new DownloadHandlerBuffer();
            await Send(request);
            if (await RetryUnauthorized(request, path, token, version, attempt)) continue;
            if (ThrowIfFailed(request, allowNotFound: true)) return default;

            string body = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(body)) return default;
            try
            {
                T value = await Task.Run(() => JsonConvert.DeserializeObject<T>(body, Json));
                EnsureSession(path, version);
                return value;
            }
            catch (ODApiException) { throw; }
            catch (Exception error)
            {
                throw new ODApiException(ODApiErrorKind.InvalidResponse, request.url, 200,
                    $"Invalid JSON for {typeof(T).Name}: {error.Message}", error);
            }
        }
        return default;
    }

    /// <summary>Stream a replay to disk with cancellation and a stall timeout. Reopen the file on a single 401 retry.</summary>
    public static async Task Download(string path, string filePath, IProgress<ulong> progress, CancellationToken token)
    {
        int version = ODAuthService.Version;
        var config = ODApiConfig.Instance;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string bearer = await TokenFor(path);
            EnsureSession(path, version);
            using var request = CreateRequest(path, 0, bearer);
            request.downloadHandler = new DownloadHandlerFile(filePath) { removeFileOnAbort = true };
            var operation = request.SendWebRequest();
            ulong received = 0;
            float lastByteAt = Time.realtimeSinceStartup;
            bool stalled = false;
            while (!operation.isDone)
            {
                if (token.IsCancellationRequested || (Protected(path) && version != ODAuthService.Version))
                { request.Abort(); break; }
                if (request.downloadedBytes != received)
                {
                    received = request.downloadedBytes;
                    lastByteAt = Time.realtimeSinceStartup;
                    progress?.Report(received);
                }
                else if (Time.realtimeSinceStartup - lastByteAt > Mathf.Max(1, config.downloadStallSeconds))
                { stalled = true; request.Abort(); break; }
                await Task.Yield();
            }
            token.ThrowIfCancellationRequested();
            EnsureSession(path, version);
            if (stalled)
                throw new ODApiException(ODApiErrorKind.Unreachable, request.url, 0, $"No data for {config.downloadStallSeconds}s");
            if (await RetryUnauthorized(request, path, bearer, version, attempt)) continue;
            ThrowIfFailed(request, allowNotFound: false);
            progress?.Report(request.downloadedBytes);
            return;
        }
    }

    static bool Protected(string path) => path != "/health" && !path.StartsWith("/health/", StringComparison.Ordinal);

    static async Task<string> TokenFor(string path)
    {
        if (!Protected(path)) return null;
        try { return await ODAuthService.RequireTokenAsync(); }
        catch { throw new ODApiException(ODApiErrorKind.Unauthorized, ODApiConfig.Instance.BaseUrl + path, 401, "Sign in to continue."); }
    }

    static void EnsureSession(string path, int version)
    {
        if (Protected(path) && (version != ODAuthService.Version || !ODAuthService.IsAuthenticated))
            throw new ODApiException(ODApiErrorKind.Unauthorized, ODApiConfig.Instance.BaseUrl + path, 401, "The session has changed.");
    }

    static async Task<bool> RetryUnauthorized(UnityWebRequest request, string path, string token, int version, int attempt)
    {
        EnsureSession(path, version);
        if (!Protected(path) || request.responseCode != 401) return false;
        if (attempt == 0 && await ODAuthService.RecoverUnauthorizedAsync(token))
        { EnsureSession(path, version); return true; }
        if (version == ODAuthService.Version) ODAuthService.Logout();
        throw new ODApiException(ODApiErrorKind.Unauthorized, request.url, 401, "Your session expired. Please sign in again.");
    }

    static UnityWebRequest CreateRequest(string path, int timeoutSeconds, string token)
    {
        var request = new UnityWebRequest(ODApiConfig.Instance.BaseUrl + path, UnityWebRequest.kHttpVerbGET);
        request.timeout = Mathf.Max(0, timeoutSeconds);
        request.SetRequestHeader("Accept", "application/json");
        if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
        return request;
    }

    static async Task Send(UnityWebRequest request)
    {
        try { await request.SendWebRequest(); }
        catch (Exception error)
        { throw new ODApiException(ODApiErrorKind.Unreachable, request.url, 0, "Cannot reach the backend.", error); }
    }

    static bool ThrowIfFailed(UnityWebRequest request, bool allowNotFound)
    {
        long status = request.responseCode;
        switch (request.result)
        {
            case UnityWebRequest.Result.Success: return false;
            case UnityWebRequest.Result.ProtocolError:
                if (allowNotFound && status == 404) return true;
                var error = ReadError(request);
                throw new ODApiException(status == 401 ? ODApiErrorKind.Unauthorized : ODApiErrorKind.HttpError,
                    request.url, status, $"HTTP {status} — {error?.message ?? "no details"}", code: error?.code);
            default:
                throw new ODApiException(ODApiErrorKind.Unreachable, request.url, status, request.error);
        }
    }

    static ApiErrorDto ReadError(UnityWebRequest request)
    {
        if (!(request.downloadHandler is DownloadHandlerBuffer buffer)) return null;
        try { return JsonConvert.DeserializeObject<ApiErrorBodyDto>(buffer.text, Json)?.error; }
        catch { return null; }
    }
}
