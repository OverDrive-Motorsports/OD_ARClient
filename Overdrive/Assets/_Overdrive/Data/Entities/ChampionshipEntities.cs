/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipEntities - Rarely-changing objects of the championship
 ## domain: catalog, calendar, sessions, drivers, teams. Mutated in place by
 ## Mappers/ChampionshipMappers.cs. See Entities/StandingEntry.cs for the
 ## one entity refreshed often.
 ##
 */

// Championship - one racing series.
public class Championship
{
    public string id;
    public string code;
    public string name;
    public string provider;
    public string category;
    public bool isActive;
}

// ChampionshipEvent - one calendar event. Same fields whether fetched from
// the calendar list or the single-event detail route (backend confirmed
// identical shape) - one mapper overload is enough.
public class ChampionshipEvent
{
    public string id;
    public string championshipId;
    public string championshipCode;
    public int? seasonYear;
    public int? roundNumber;
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

// RaceSession - one practice/qualifying/race/sprint session. Same fields
// whether fetched from the session list or the single-session detail route.
public class RaceSession
{
    public string id;
    public string eventId;
    public string type;
    public string status;
    public string name;
    public string circuit;
    public string broadcastUrl;
    public string startTime;
    public string endTime;
}

// Driver - fed by both SessionDriverDTO (has firstName/lastName/code/teamColor,
// no nationality) and DriverProfileDTO (has nationality/championshipCode, no
// team names) - a driver seen only in a session list still gets enriched once
// its profile is fetched.
public class Driver
{
    public int number;
    public string fullName;
    public string firstName;
    public string lastName;
    public string code;
    public string teamId;
    public string teamName;
    public string teamColor;
    public string nationality;
    public string championshipCode;
}

// Team - one team engaged in a session.
public class Team
{
    public string id;
    public string name;
    public string code;
    public string color;
}
