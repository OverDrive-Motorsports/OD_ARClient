/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipMappers - Applies championship DTOs onto existing Entities.
 ## Every TryApply mutates the "target" passed in and never creates one
 ## (creation only happens once per key, in Mappers/EntityCollectionSync.cs).
 ## Returns null on success, a DataError (kind Validation) when the entity's
 ## identifying field is missing/inconsistent - cosmetic fields are never validated.
 ##
 */

// Championship
public static class ChampionshipMapper
{
    public static DataError? TryApply(this ChampionshipDTO dto, Championship target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.championshipCode))
            return new DataError(DataErrorKind.Validation, "ChampionshipMapper", "championship missing championshipCode");

        if (!string.IsNullOrEmpty(target.code) && target.code != dto.championshipCode)
            return new DataError(DataErrorKind.Validation, "ChampionshipMapper",
                $"championship code mismatch: target is '{target.code}', dto is '{dto.championshipCode}'");

        target.id = dto.id;
        target.code = dto.championshipCode;
        target.name = dto.name;
        target.provider = dto.provider;
        target.category = dto.category;
        target.isActive = dto.isActive;
        return null;
    }
}

// ChampionshipEvent - one shape covers both the calendar list and the single-event detail route.
public static class ChampionshipEventMapper
{
    public static DataError? TryApply(this EventDTO dto, ChampionshipEvent target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.eventId))
            return new DataError(DataErrorKind.Validation, "ChampionshipEventMapper", "event missing eventId");

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.eventId)
            return new DataError(DataErrorKind.Validation, "ChampionshipEventMapper",
                $"event id mismatch: target is '{target.id}', dto is '{dto.eventId}'");

        target.id = dto.eventId;
        target.championshipId = dto.championshipId;
        target.championshipCode = dto.championshipCode;
        target.seasonYear = dto.seasonYear;
        target.roundNumber = dto.roundNumber;
        target.name = dto.name;
        target.officialName = dto.officialName;
        target.circuit = dto.circuit;
        target.location = dto.location;
        target.countryName = dto.countryName;
        target.countryCode = dto.countryCode;
        target.status = dto.status;
        target.startDate = dto.startDate;
        target.endDate = dto.endDate;
        return null;
    }
}

// RaceSession - one shape covers both the session list and the single-session detail route.
public static class RaceSessionMapper
{
    public static DataError? TryApply(this SessionDTO dto, RaceSession target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.sessionId))
            return new DataError(DataErrorKind.Validation, "RaceSessionMapper", "session missing sessionId");

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.sessionId)
            return new DataError(DataErrorKind.Validation, "RaceSessionMapper",
                $"session id mismatch: target is '{target.id}', dto is '{dto.sessionId}'");

        target.id = dto.sessionId;
        target.eventId = dto.eventId;
        target.type = dto.type;
        target.status = dto.status;
        target.name = dto.name;
        target.circuit = dto.circuit;
        target.broadcastUrl = dto.broadcastUrl;
        target.startTime = dto.startTime;
        target.endTime = dto.endTime;
        return null;
    }
}

// Driver - two overloads, one per endpoint shape (SessionDriverDTO/DriverProfileDTO).
public static class DriverMapper
{
    public static DataError? TryApply(this SessionDriverDTO dto, Driver target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "DriverMapper", "driver missing driverNumber");

        if (target.number != 0 && target.number != dto.driverNumber)
            return new DataError(DataErrorKind.Validation, "DriverMapper",
                $"driver number mismatch: target is '{target.number}', dto is '{dto.driverNumber}'");

        target.number = dto.driverNumber;
        target.fullName = dto.fullName;
        target.firstName = dto.firstName;
        target.lastName = dto.lastName;
        target.code = dto.code;
        target.teamId = dto.teamId;
        target.teamName = dto.teamName;
        target.teamColor = dto.teamColor;
        return null;
    }

    public static DataError? TryApply(this DriverProfileDTO dto, Driver target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "DriverMapper", "driver missing driverNumber");

        if (target.number != 0 && target.number != dto.driverNumber)
            return new DataError(DataErrorKind.Validation, "DriverMapper",
                $"driver number mismatch: target is '{target.number}', dto is '{dto.driverNumber}'");

        target.number = dto.driverNumber;
        target.fullName = dto.fullName;
        target.nationality = dto.nationality;
        target.teamId = dto.currentTeamId;
        target.championshipCode = dto.championshipCode;
        // firstName/lastName/code/teamName/teamColor: not carried by DriverProfileDTO, stay untouched.
        return null;
    }
}

// Team
public static class TeamMapper
{
    public static DataError? TryApply(this TeamDTO dto, Team target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.teamId))
            return new DataError(DataErrorKind.Validation, "TeamMapper", "team missing teamId");

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.teamId)
            return new DataError(DataErrorKind.Validation, "TeamMapper",
                $"team id mismatch: target is '{target.id}', dto is '{dto.teamId}'");

        target.id = dto.teamId;
        target.name = dto.name;
        target.code = dto.code;
        target.color = dto.color;
        return null;
    }
}

// StandingEntry - the one entity in this file that lives in Entities/Live (refreshed often).
public static class StandingEntryMapper
{
    public static DataError? TryApply(this StandingEntryDTO dto, StandingEntry target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "StandingEntryMapper", "standing entry missing driverNumber");

        if (target.driverNumber != 0 && target.driverNumber != dto.driverNumber)
            return new DataError(DataErrorKind.Validation, "StandingEntryMapper",
                $"standing entry driver mismatch: target is '{target.driverNumber}', dto is '{dto.driverNumber}'");

        target.position = dto.position;
        target.driverNumber = dto.driverNumber;
        target.teamId = dto.teamId;
        target.gapToLeader = dto.gapToLeader;
        target.points = dto.points;
        return null;
    }
}
