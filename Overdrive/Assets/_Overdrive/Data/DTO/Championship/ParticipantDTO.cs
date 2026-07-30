/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ParticipantDTO - Mirrors driver/team/standings endpoints. Driver has two
 ## shapes (session list vs global profile) with different field subsets,
 ## same pattern as CatalogDTO/SessionDTO.
 ##
 */

using System;

// GET /sessions/{sessionId}/drivers (one entry)
[Serializable]
public class SessionDriverDTO
{
    public string id;
    public int driverNumber;
    public string fullName;
    public string firstName;
    public string lastName;
    public string code;
    public string teamId;
    public string teamName;
    public string teamColor;
}

// GET /drivers/{driverNumber}/profile - global, not scoped to a session.
// driverPicture doesn't exist on this response (no data source) - not modeled.
[Serializable]
public class DriverProfileDTO
{
    public int driverNumber;
    public string fullName;
    public string nationality;
    public string currentTeamId;
    public string championshipCode;
}

// GET /sessions/{sessionId}/teams (one entry)
[Serializable]
public class TeamDTO
{
    public string teamId;
    public string name;
    public string code;
    public string color;
}

// GET /sessions/{sessionId}/standings (one entry). Known gap: for a DNF
// driver, position is 0 and gapToLeader/points are absent from the JSON
// (not just null) - they'll just be default (null/0) on the DTO.
[Serializable]
public class StandingEntryDTO
{
    public int position;
    public int driverNumber;
    public string teamId;
    public string gapToLeader;
    public int points;
}
