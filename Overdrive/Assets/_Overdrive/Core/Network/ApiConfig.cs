/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ApiConfig - Single source of truth for the gateway base URL and the
 ## optional Bearer token forwarded on every request. The gateway currently
 ## accepts requests with no Authorization header at all (see gateway/ROUTES.md
 ## - "Auth Behavior"), so AuthToken is null/empty by default; setting it later
 ## (once the team enforces auth) requires no change anywhere else, since
 ## ApiClient reads it on every call.
 ##
 */

public static class ApiConfig
{
    // Gateway entrypoint. Defaults to the local gateway (HTTP_PORT default template = 3000).
    public static string BaseUrl = "http://localhost:3000";

    // Bearer token sent as "Authorization: Bearer {AuthToken}" when non-empty. Optional for now.
    public static string AuthToken = null;
}
