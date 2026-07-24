/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipEntities - Rarely-changing objects of the championship
 ## domain: catalog, calendar, sessions, drivers, teams. Mutated in place by
 ## Mappers/ChampionshipMappers.cs. See StandingEntry.cs for the one entity
 ## refreshed often.
 ##
 */

// Championship - one racing series/season.
public class Championship
{
    public string code;
    public string name;
    public string provider;
    public int season;
}

// ChampionshipEvent - one calendar event. Fed by both EventSummaryDTO and
// EventDetailDTO, each only touching the fields its DTO carries.
public class ChampionshipEvent
{
    public string id;
    public string championshipCode;
    public string name;
    public string location;
    public string circuit;
    public string startDate;
    public string endDate;
    public string status;
}

// RaceSession - one practice/qualifying/race/sprint session. Same
// summary/detail merge rule as ChampionshipEvent.
public class RaceSession
{
    public string id;
    public string eventId;
    public string type;
    public string status;
    public string circuit;
    public string startTime;
    public string endTime;
    public SessionWeather weather;
}

// Nested data, not fetched on its own - always attached to a RaceSession.
public class SessionWeather
{
    public float airTemperature;
    public float trackTemperature;
    public bool rainfall;
}

// Driver - fed by both SessionDriverDTO and DriverProfileDTO, so a driver
// seen only in a session list still gets enriched once its profile is fetched.
public class Driver
{
    public int number;
    public string fullName;
    public string teamId;
    public string teamName;
    public string nationality;
    public string championshipCode;
    public string pictureUrl;
}

// Team - one team engaged in a session.
public class Team
{
    public string id;
    public string name;
    public string color;
}
