/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## SessionDTO - Mirrors GET /events/{eventId}/sessions (array) and
 ## GET /sessions/{sessionId} (single object) - same shape both ways per the
 ## backend. No nested weather anymore (weatherAtStart was dropped from the
 ## contract - see RaceWeatherDTO for the unrelated race/weather time series).
 ##
 */

using System;

[Serializable]
public class SessionDTO
{
    public string sessionId;
    public string eventId;
    public string type;
    public string status;
    public string name;
    public string circuit;
    public string broadcastUrl; // usually null
    public string startTime;
    public string endTime;
}
