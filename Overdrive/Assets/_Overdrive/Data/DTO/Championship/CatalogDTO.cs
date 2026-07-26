/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## CatalogDTO - Mirrors the "browse" endpoints: championship list and
 ## calendar. EventDTO covers both GET /championships/{code}/events (array)
 ## and GET /events/{eventId} (single object) - the backend confirmed both
 ## return the exact same fields, just singular vs array.
 ##
 */

using System;

// GET /championships (one entry)
[Serializable]
public class ChampionshipDTO
{
    public string id;
    public string championshipCode;
    public string name;
    public string provider;
    public string category;
    public bool isActive;
}

// GET /championships/{code}/events (array) and GET /events/{eventId} (single object) - same shape.
[Serializable]
public class EventDTO
{
    public string eventId;
    public string championshipId;
    public string championshipCode;
    public int? seasonYear;
    public int? roundNumber; // always null for now - not provided by OpenF1 on this resource
    public string name;
    public string officialName;
    public string circuit;
    public string location;
    public string countryName;
    public string countryCode;
    public string status;
    public string startDate;
    public string endDate;
}
