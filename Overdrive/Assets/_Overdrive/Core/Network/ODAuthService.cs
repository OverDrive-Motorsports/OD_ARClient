using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>User session only. Passwords are never persisted; backend responses and tokens are never logged.</summary>
public static class ODAuthService
{
    public static bool IsAuthenticated { get; private set; }
    public static string Email { get; private set; }
    public static string AccessToken => IsAuthenticated ? _session?.Token : null;
    public static int Version { get; private set; }
    public static event Action Changed;
    static Session _session;
    static Task<bool> _refresh;
    static string Scope => ODApiConfig.Instance.AuthBaseUrl + "|" + ODApiConfig.Instance.BaseUrl;

    sealed class Session
    {
        public string Token, RefreshToken, Id, Email, Scope;
        public DateTime ExpiresUtc;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsAuthenticated = false; Email = null; _session = null; _refresh = null; Changed = null; Version = 0;
    }

    /// <summary>Always revalidate saved credentials with the backend before opening any application window.</summary>
    public static async Task<bool> RestoreAsync()
    {
        int version = Version;
        try
        {
            string json = ODAuthStorage.Read();
            if (string.IsNullOrEmpty(json)) return false;
            var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<Session>(json);
            if (saved == null || saved.Scope != Scope || string.IsNullOrWhiteSpace(saved.Id) || string.IsNullOrWhiteSpace(saved.RefreshToken))
            { ODAuthStorage.Clear(); return false; }
            if (version != Version) return false;
            _session = saved; Email = saved.Email;
            return await RefreshAsync();
        }
        catch { if (version == Version) Logout(); return false; }
    }

    public static async Task LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Enter your email and password.");
        Logout();
        int version = Version;
        string scope = Scope;
        var data = await SendAsync("/login", new JObject { ["email"] = email.Trim(), ["password"] = password });
        var session = Parse(data, null);
        if (version != Version) throw new InvalidOperationException("Sign-in cancelled. Please try again.");
        if (scope != Scope) throw new InvalidOperationException("The server configuration changed. Please sign in again.");
        session.Email = email.Trim(); session.Scope = scope;
        Commit(session);
    }

    public static Task<bool> RefreshAsync()
    {
        if (_refresh != null) return _refresh;
        return _refresh = RefreshCoreAsync();
    }

    static async Task<bool> RefreshCoreAsync()
    {
        int version = Version;
        var previous = _session;
        try
        {
            if (previous == null || previous.Scope != Scope) { Logout(); return false; }
            var data = await SendAsync("/refresh", new JObject { ["sessionId"] = previous.Id, ["refreshToken"] = previous.RefreshToken });
            var session = Parse(data, previous.Id);
            if (version != Version) return false;
            session.Email = previous.Email; session.Scope = previous.Scope;
            Commit(session);
            return true;
        }
        catch { if (version == Version) Logout(); return false; }
        finally { if (version == Version) _refresh = null; }
    }

    public static async Task<string> RequireTokenAsync()
    {
        if (!IsAuthenticated || _session == null) throw new InvalidOperationException("Sign in to continue.");
        if (_session.Scope != Scope) { Logout(); throw new InvalidOperationException("The server configuration changed. Please sign in again."); }
        if (_session.ExpiresUtc <= DateTime.UtcNow.AddSeconds(30) && !await RefreshAsync())
            throw new InvalidOperationException("Your session expired. Please sign in again.");
        return AccessToken;
    }

    public static async Task<bool> RecoverUnauthorizedAsync(string rejectedToken)
    {
        if (!IsAuthenticated) return false;
        // Another request may already have rotated this token. Do not refresh twice.
        if (AccessToken != rejectedToken) return true;
        return await RefreshAsync();
    }

    static void Commit(Session session)
    {
        _session = session; Email = session.Email;
        // A storage failure must not leave a live refresh token in an unprotected fallback file.
        try { ODAuthStorage.Write(Newtonsoft.Json.JsonConvert.SerializeObject(session)); }
        catch { ODAuthStorage.Clear(); }
        bool changed = !IsAuthenticated;
        IsAuthenticated = true;
        if (changed) Changed?.Invoke();
    }

    public static void Logout()
    {
        ++Version; _session = null; _refresh = null; Email = null; IsAuthenticated = false;
        ODAuthStorage.Clear(); Changed?.Invoke();
    }

    static Session Parse(JObject data, string previousId)
    {
        string token = (string)(data["token"] ?? data["accessToken"]);
        string refresh = (string)(data["refreshToken"] ?? data["refresh_token"]);
        string id = (string)(data["sessionId"] ?? data["session_id"] ?? data["session"]) ?? previousId;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refresh) || string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("The authentication service returned an incomplete session.");
        DateTime expires = DateTime.MaxValue;
        string rawExpiry = (string)(data["expiresAt"] ?? data["expires_at"]);
        if (DateTime.TryParse(rawExpiry, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date)) expires = date;
        // expiresAt can describe the refresh session. The JWT's exp is the access-token deadline.
        try
        {
            string payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            long? seconds = (long?)JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)))["exp"];
            if (seconds.HasValue)
            {
                var jwtExpiry = DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime;
                if (jwtExpiry < expires) expires = jwtExpiry;
            }
        }
        catch { throw new InvalidOperationException("The authentication service returned an invalid access token."); }
        if (expires <= DateTime.UtcNow) throw new InvalidOperationException("The authentication service returned an expired session.");
        return new Session { Token = token, RefreshToken = refresh, Id = id, ExpiresUtc = expires };
    }

    static async Task<JObject> SendAsync(string path, JObject payload)
    {
        string url = ODApiConfig.Instance.AuthBaseUrl + path;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            throw new InvalidOperationException("Check the authentication service URL in API Config.");
        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.timeout = Mathf.Max(1, ODApiConfig.Instance.timeoutSeconds);
        try { await request.SendWebRequest(); }
        catch { throw new InvalidOperationException("Cannot reach the authentication service. Please try again."); }
        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
                throw new InvalidOperationException("Email or password incorrect, or session expired.");
            if (request.responseCode == 429) throw new InvalidOperationException("Too many attempts. Please wait and try again.");
            throw new InvalidOperationException("Authentication unavailable. Check the service address and try again.");
        }
        try { return JObject.Parse(request.downloadHandler.text); }
        catch { throw new InvalidOperationException("The authentication service returned an invalid response."); }
    }
}
