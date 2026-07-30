/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## GatewayHealthApi - Gateway's own liveness endpoint (see gateway/ROUTES.md
 ## - "Health Routes", GET /health). Per-service probes (/health/championship,
 ## etc.) will be added here once we need them.
 ##
 */

using System;
using System.Collections;

public static class GatewayHealthApi
{
    // Checks the gateway process itself is up (not the upstream services behind it).
    public static IEnumerator GetGatewayHealth(Action<HealthStatusDTO> onSuccess, Action<DataError> onError)
        => ApiClient.Get("/health", onSuccess, onError);
}
