/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipMappers - Applies championship DTOs onto existing Entities.
 ## Every TryApply mutates the "target" passed in and never creates one
 ## (creation only happens once per key, in Mappers/EntityCollectionSync.cs).
 ## Returns null on success, an error message when the entity's identifying
 ## field is missing/inconsistent - cosmetic fields are never validated.
 ##
 */

// Championship
public static class ChampionshipMapper
{
    public static string TryApply(this ChampionshipDTO dto, Championship target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.championshipCode))
            return "championship missing championshipCode";

        if (!string.IsNullOrEmpty(target.code) && target.code != dto.championshipCode)
            return $"championship code mismatch: target is '{target.code}', dto is '{dto.championshipCode}'";

        target.code = dto.championshipCode;
        target.name = dto.name;
        target.provider = dto.provider;
        target.season = dto.season;
        return null;
    }
}

// ChampionshipEvent - two overloads, one per endpoint shape (EventSummaryDTO/EventDetailDTO).
public static class ChampionshipEventMapper
{
    public static string TryApply(this EventSummaryDTO dto, ChampionshipEvent target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.eventId))
            return "event missing eventId";

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.eventId)
            return $"event id mismatch: target is '{target.id}', dto is '{dto.eventId}'";

        target.id = dto.eventId;
        target.name = dto.name;
        target.location = dto.location;
        target.startDate = dto.startDate;
        target.endDate = dto.endDate;
        target.status = dto.status;
        return null;
    }

    public static string TryApply(this EventDetailDTO dto, ChampionshipEvent target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.eventId))
            return "event missing eventId";

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.eventId)
            return $"event id mismatch: target is '{target.id}', dto is '{dto.eventId}'";

        target.id = dto.eventId;
        target.championshipCode = dto.championshipCode;
        target.name = dto.name;
        target.circuit = dto.circuit;
        target.location = dto.location;
        target.startDate = dto.startDate;
        target.endDate = dto.endDate;
        // status: not carried by EventDetailDTO, whatever EventSummaryDTO already set stays untouched.
        return null;
    }
}

// RaceSession - two overloads, one per endpoint shape (SessionSummaryDTO/SessionDetailDTO).
public static class RaceSessionMapper
{
    public static string TryApply(this SessionSummaryDTO dto, RaceSession target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.sessionId))
            return "session missing sessionId";

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.sessionId)
            return $"session id mismatch: target is '{target.id}', dto is '{dto.sessionId}'";

        target.id = dto.sessionId;
        target.type = dto.type;
        target.status = dto.status;
        target.startTime = dto.startTime;
        target.endTime = dto.endTime;
        return null;
    }

    public static string TryApply(this SessionDetailDTO dto, RaceSession target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.sessionId))
            return "session missing sessionId";

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.sessionId)
            return $"session id mismatch: target is '{target.id}', dto is '{dto.sessionId}'";

        target.id = dto.sessionId;
        target.eventId = dto.eventId;
        target.type = dto.type;
        target.status = dto.status;
        target.circuit = dto.circuit;
        target.startTime = dto.startTime;
        target.endTime = dto.endTime;

        if (dto.weatherAtStart != null)
        {
            target.weather ??= new SessionWeather();
            target.weather.airTemperature = dto.weatherAtStart.airTemperature;
            target.weather.trackTemperature = dto.weatherAtStart.trackTemperature;
            target.weather.rainfall = dto.weatherAtStart.rainfall;
        }
        return null;
    }
}

// Driver - two overloads, one per endpoint shape (SessionDriverDTO/DriverProfileDTO).
public static class DriverMapper
{
    public static string TryApply(this SessionDriverDTO dto, Driver target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return "driver missing driverNumber";

        if (target.number != 0 && target.number != dto.driverNumber)
            return $"driver number mismatch: target is '{target.number}', dto is '{dto.driverNumber}'";

        target.number = dto.driverNumber;
        target.fullName = dto.fullName;
        target.teamId = dto.teamId;
        target.teamName = dto.teamName;
        return null;
    }

    public static string TryApply(this DriverProfileDTO dto, Driver target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return "driver missing driverNumber";

        if (target.number != 0 && target.number != dto.driverNumber)
            return $"driver number mismatch: target is '{target.number}', dto is '{dto.driverNumber}'";

        target.number = dto.driverNumber;
        target.fullName = dto.fullName;
        target.nationality = dto.nationality;
        target.teamId = dto.currentTeamId;
        target.championshipCode = dto.championshipCode;
        target.pictureUrl = dto.driverPicture;
        // teamName: not carried by DriverProfileDTO, whatever SessionDriverDTO already set stays untouched.
        return null;
    }
}

// Team
public static class TeamMapper
{
    public static string TryApply(this TeamDTO dto, Team target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.teamId))
            return "team missing teamId";

        if (!string.IsNullOrEmpty(target.id) && target.id != dto.teamId)
            return $"team id mismatch: target is '{target.id}', dto is '{dto.teamId}'";

        target.id = dto.teamId;
        target.name = dto.name;
        target.color = dto.color;
        return null;
    }
}

// StandingEntry - the one entity in this file that lives in Entities/Live (refreshed often).
public static class StandingEntryMapper
{
    public static string TryApply(this StandingEntryDTO dto, StandingEntry target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return "standing entry missing driverNumber";

        if (target.driverNumber != 0 && target.driverNumber != dto.driverNumber)
            return $"standing entry driver mismatch: target is '{target.driverNumber}', dto is '{dto.driverNumber}'";

        target.position = dto.position;
        target.driverNumber = dto.driverNumber;
        target.teamId = dto.teamId;
        target.gapToLeader = dto.gapToLeader;
        target.points = dto.points;
        return null;
    }
}
