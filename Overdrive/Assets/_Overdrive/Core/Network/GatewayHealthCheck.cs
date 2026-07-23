/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## GatewayHealthCheck - Manual connectivity test: drop this on any GameObject
 ## in a scene to call GET /health against the gateway on Start and log the
 ## result. Not wired into app startup - purely a plumbing check while there is
 ## no real feature consuming Core/Network yet.
 ##
 */

using UnityEngine;

public class GatewayHealthCheck : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(GatewayHealthApi.GetGatewayHealth(
            data => Debug.Log($"[GatewayHealthCheck] Gateway reachable at {ApiConfig.BaseUrl} - status: {data.status}"),
            error => Debug.LogError($"[GatewayHealthCheck] {error}")
        ));
    }
}
