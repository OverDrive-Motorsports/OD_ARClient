/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ParticipantDTO - Mirrors driver/team/standings endpoints. Driver has two
 ## shapes (session list vs profile) with different field subsets, same
 ## pattern as CatalogDTO/SessionDTO.
 ##
 */

using System;

// GET /sessions/{sessionId}/drivers (one entry) - has teamName, no nationality/picture.
[Serializable]
public class SessionDriverDTO
{
    public int driverNumber;
    public string fullName;
    public string teamId;
    public string teamName;
}

// GET /drivers/{driverNumber}/profile - has nationality/picture, no teamName.
[Serializable]
public class DriverProfileDTO
{
    public int driverNumber;
    public string fullName;
    public string nationality;
    public string currentTeamId;
    public string championshipCode;
    public string driverPicture;
}

// GET /sessions/{sessionId}/teams (one entry)
[Serializable]
public class TeamDTO
{
    public string teamId;
    public string name;
    public string color;
}

// GET /sessions/{sessionId}/standings (one entry)
[Serializable]
public class StandingEntryDTO
{
    public int position;
    public int driverNumber;
    public string teamId;
    public string gapToLeader;
    public int points;
}
