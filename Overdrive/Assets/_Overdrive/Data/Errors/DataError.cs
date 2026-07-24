/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## DataError - Centralizes backend-communication error handling: DTOs,
 ## Mappers and Core/Network all report through this one type instead of
 ## each logging its own ad-hoc string. Report() is the single place that
 ## decides severity per kind - change the policy here, not at every call site.
 ##
 */

using UnityEngine;

public enum DataErrorKind
{
    Network,      // request unreachable, timed out, or non-2xx HTTP
    Deserialize,  // response body isn't valid/expected JSON
    Validation,   // mapper: an entity's identifying field is missing/inconsistent
}

public readonly struct DataError
{
    public readonly DataErrorKind kind;
    public readonly string source;   // e.g. "ApiClient", "ChampionshipMapper"
    public readonly string message;

    public DataError(DataErrorKind kind, string source, string message)
    {
        this.kind = kind;
        this.source = source;
        this.message = message;
    }

    public override string ToString() => $"[{source}] {message}";

    // Validation errors mean one entry was skipped - the rest of the batch is still fine.
    // Network/Deserialize errors mean the whole call failed - nothing to show instead.
    public void Report()
    {
        if (kind == DataErrorKind.Validation) Debug.LogWarning(ToString());
        else Debug.LogError(ToString());
    }
}
