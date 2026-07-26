/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceMappers - Applies "Live - Course" DTOs onto existing Entities. Same
 ## rules as ChampionshipMappers.cs: TryApply mutates "target" and never
 ## creates one, returns null on success or a DataError (kind Validation)
 ## when the identifying field is missing/inconsistent.
 ##
 */

// RacePosition
public static class RacePositionMapper
{
    public static DataError? TryApply(this RacePositionDTO dto, RacePosition target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "RacePositionMapper", "race position missing driverNumber");

        if (target.driverNumber != 0 && target.driverNumber != dto.driverNumber)
            return new DataError(DataErrorKind.Validation, "RacePositionMapper",
                $"race position driver mismatch: target is '{target.driverNumber}', dto is '{dto.driverNumber}'");

        target.driverNumber = dto.driverNumber;
        target.position = dto.position;
        target.gapToLeader = dto.gapToLeader;
        target.gapAhead = dto.gapAhead;
        target.gapBehind = dto.gapBehind;
        target.lapsCompleted = dto.lapsCompleted;
        target.timestamp = dto.timestamp;
        return null;
    }
}

// DriverLaps - laps list is rebuilt wholesale each time (a recorded lap doesn't change afterwards).
public static class DriverLapsMapper
{
    public static DataError? TryApply(this RaceLapsDTO dto, DriverLaps target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "DriverLapsMapper", "driver laps missing driverNumber");

        if (target.driverNumber != 0 && target.driverNumber != dto.driverNumber)
            return new DataError(DataErrorKind.Validation, "DriverLapsMapper",
                $"driver laps mismatch: target is '{target.driverNumber}', dto is '{dto.driverNumber}'");

        target.driverNumber = dto.driverNumber;
        target.bestLap = dto.bestLap;
        target.averageLap = dto.averageLap;

        target.laps.Clear();
        if (dto.laps != null)
        {
            foreach (var lapDto in dto.laps)
            {
                target.laps.Add(new Lap
                {
                    lapNumber = lapDto.lapNumber,
                    lapDuration = lapDto.lapDuration,
                    sector1 = lapDto.sector1,
                    sector2 = lapDto.sector2,
                    sector3 = lapDto.sector3,
                    isPitOutLap = lapDto.isPitOutLap,
                });
            }
        }
        return null;
    }
}

// Stint
public static class StintMapper
{
    public static DataError? TryApply(this StintDTO dto, Stint target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "StintMapper", "stint missing driverNumber");

        target.driverNumber = dto.driverNumber;
        target.stintNumber = dto.stintNumber;
        target.compound = dto.compound;
        target.lapStart = dto.lapStart;
        target.lapEnd = dto.lapEnd;
        target.tyreAgeAtStart = dto.tyreAgeAtStart;
        return null;
    }
}

// PitStop
public static class PitStopMapper
{
    public static DataError? TryApply(this PitStopDTO dto, PitStop target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "PitStopMapper", "pit stop missing driverNumber");

        target.driverNumber = dto.driverNumber;
        target.lapNumber = dto.lapNumber;
        target.pitDuration = dto.pitDuration;
        return null;
    }
}

// RaceControlEvent - keyed by timestamp (no other unique id in the source shape).
public static class RaceControlEventMapper
{
    public static DataError? TryApply(this RaceControlEventDTO dto, RaceControlEvent target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.timestamp))
            return new DataError(DataErrorKind.Validation, "RaceControlEventMapper", "race control event missing timestamp");

        target.category = dto.category;
        target.flag = dto.flag;
        target.safetyCar = dto.safetyCar;
        target.message = dto.message;
        target.lapNumber = dto.lapNumber;
        target.timestamp = dto.timestamp;

        if (dto.penality != null)
        {
            target.penalty ??= new Penalty();
            target.penalty.driverNumber = dto.penality.driverNumber;
            target.penalty.timePenalty = dto.penality.timePenalty;
        }
        else
        {
            target.penalty = null;
        }
        return null;
    }
}

// WeatherSample - keyed by timestamp.
public static class WeatherSampleMapper
{
    public static DataError? TryApply(this RaceWeatherSampleDTO dto, WeatherSample target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.timestamp))
            return new DataError(DataErrorKind.Validation, "WeatherSampleMapper", "weather sample missing timestamp");

        target.timestamp = dto.timestamp;
        target.airTemperature = dto.airTemperature;
        target.trackTemperature = dto.trackTemperature;
        target.humidity = dto.humidity;
        target.windSpeed = dto.windSpeed;
        target.rainfall = dto.rainfall;
        return null;
    }
}

// TeamRadioMessage - keyed by audioUrl (unique per clip).
public static class TeamRadioMessageMapper
{
    public static DataError? TryApply(this TeamRadioMessageDTO dto, TeamRadioMessage target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.audioUrl))
            return new DataError(DataErrorKind.Validation, "TeamRadioMessageMapper", "team radio message missing audioUrl");

        target.driverNumber = dto.driverNumber;
        target.timestamp = dto.timestamp;
        target.audioUrl = dto.audioUrl;
        return null;
    }
}
