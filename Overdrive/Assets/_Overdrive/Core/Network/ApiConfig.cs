/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ApiConfig - Single source of truth for the gateway base URL and the
 ## Bearer token forwarded on every request. Auth is now mandatory on every
 ## route (confirmed by the backend's endpoints doc) - AuthToken defaults to
 ## null here only because nothing has wired up a real token yet; every
 ## call will 401 until it's set. Setting it (e.g. from a login/config
 ## screen) requires no change anywhere else, since ApiClient reads it on
 ## every call.
 ##
 */

public static class ApiConfig
{
    // Gateway entrypoint. Defaults to the local gateway (HTTP_PORT default template = 3000).
    public static string BaseUrl = "http://localhost:3000";

    // Bearer token sent as "Authorization: Bearer {AuthToken}" when non-empty. Must be set - auth is mandatory now.
    public static string AuthToken = null;
}
