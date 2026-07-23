/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## SessionDTO - Mirrors session-related endpoints. Same summary/detail split
 ## as CatalogDTO's events: SessionDetailDTO adds eventId/circuit/weather.
 ##
 */

using System;

// GET /events/{eventId}/sessions (one entry)
[Serializable]
public class SessionSummaryDTO
{
    public string sessionId;
    public string type;
    public string status;
    public string startTime;
    public string endTime;
}

// GET /sessions/{sessionId}
[Serializable]
public class SessionDetailDTO
{
    public string sessionId;
    public string eventId;
    public string type;
    public string status;
    public string circuit;
    public string startTime;
    public string endTime;
    public WeatherDTO weatherAtStart;
}

// Nested in SessionDetailDTO.weatherAtStart, not fetched on its own.
[Serializable]
public class WeatherDTO
{
    public float airTemperature;
    public float trackTemperature;
    public bool rainfall;
}
