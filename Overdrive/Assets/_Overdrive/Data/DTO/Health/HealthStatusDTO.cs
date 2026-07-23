/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## HealthStatusDTO - Mirrors the JSON returned by the gateway's GET /health
 ## ({"status":"ok"}) and by GET /health/{service} ({"status":"ok","service":"auth"}).
 ##
 */

using System;

[Serializable]
public class HealthStatusDTO
{
    // "ok" on success — this is the only field the gateway itself returns.
    public string status;

    // Which upstream service was probed, e.g. "auth". Null for the gateway's own GET /health.
    public string service;
}
