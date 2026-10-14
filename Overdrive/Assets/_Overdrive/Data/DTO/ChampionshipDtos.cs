/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipDtos - Mirrors of championship-service JSON (gateway prefix
 ## /v1/championship). Field names match the JSON keys exactly so they
 ## deserialize without attributes: camelCase for the catalog, snake_case for
 ## dataset rows (raw OpenF1 records enriched by the service).
 ## Source of truth: services/championship-service/src/core/domain/catalog.entity.go
 ##
 */

using System;
using System.Collections.Generic;

// Every DTO name ends with "Dto": Assets/_Overdrive/Data/DTO/link.xml keeps
// them (and their fields, read by reflection) from IL2CPP code stripping.

/// <summary>GET /championships</summary>
public class ChampionshipSummaryDto
{
    public string id;
    public string championshipCode;
    public string name;
    public string provider;
    public string category;
    public int    season;
    public bool   isActive;
}

/// <summary>GET /championships/{code}/events, GET /events/{eventId}. status: scheduled | live | finished.</summary>
public class EventSummaryDto
{
    public string   eventId;
    public string   championshipId;
    public string   championshipCode;
    public int      seasonYear;
    public int?     roundNumber;
    public string   name;
    public string   officialName;
    public string   circuit;
    public string   location;
    public string   countryName;
    public string   countryCode;
    public string   status;
    public DateTime startDate;
    public DateTime endDate;
}

/// <summary>GET /events/{eventId}/sessions. type: race | sprint | quali | practice — status: scheduled | live | finished.</summary>
public class SessionSummaryDto
{
    public string    sessionId;
    public string    eventId;
    public string    type;
    public string    status;
    public string    name;
    public string    circuit;
    public string    broadcastUrl;
    public DateTime  startTime;
    public DateTime? endTime;
}

/// <summary>GET /sessions/{sessionId}/drivers</summary>
public class DriverSummaryDto
{
    public string id;
    public int    driverNumber;
    public string fullName;
    public string firstName;
    public string lastName;
    public string code;
    public string countryCode;
    public string teamId;
    public string teamName;
    public string teamColor;
}

/// <summary>GET /sessions/{sessionId}/datasets/{dataset} — the generic envelope.</summary>
public class SessionDatasetDto<TRow>
{
    public string     sessionId;
    public string     dataset;
    public int        count;
    public List<TRow> data;
}

/// <summary>Row of the "championship_drivers" dataset (OpenF1 championship_drivers + driver enrichment).</summary>
public class DriverStandingRowDto
{
    public int    driver_number;
    public int?   position_current;
    public float? points_current;
    public string driver_name;
    public string team_name;
    public string team_color;
}

/// <summary>Row of the "championship_teams" dataset (OpenF1 championship_teams).</summary>
public class TeamStandingRowDto
{
    public string team_name;
    public int?   position_current;
    public float? points_current;
}

/// <summary>Row of the "session_result" dataset (OpenF1 session_result + driver enrichment).</summary>
public class SessionResultRowDto
{
    public int    driver_number;
    public int?   position;
    public int?   number_of_laps;
    /// <summary>Seconds behind the winner, or a string like "+1 LAP" — kept raw.</summary>
    public object gap_to_leader;
    public bool   dnf;
    public bool   dns;
    public bool   dsq;
    public string driver_name;
    public string team_name;
    public string team_color;
}
