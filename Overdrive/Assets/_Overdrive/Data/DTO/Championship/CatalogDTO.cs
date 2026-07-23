/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## CatalogDTO - Mirrors the "browse" endpoints: championship list and
 ## calendar. Event has two shapes (list vs single) because the backend
 ## splits the data across two endpoints with different field subsets.
 ##
 */

using System;

// GET /championships (one entry)
[Serializable]
public class ChampionshipDTO
{
    public string championshipCode;
    public string name;
    public string provider;
    public int season;
}

// GET /championships/{code}/events (one entry) - has status but not championshipCode/circuit.
[Serializable]
public class EventSummaryDTO
{
    public string eventId;
    public string name;
    public string location;
    public string startDate;
    public string endDate;
    public string status;
}

// GET /events/{eventId} - has championshipCode/circuit but not status.
[Serializable]
public class EventDetailDTO
{
    public string eventId;
    public string championshipCode;
    public string name;
    public string circuit;
    public string location;
    public string startDate;
    public string endDate;
}
