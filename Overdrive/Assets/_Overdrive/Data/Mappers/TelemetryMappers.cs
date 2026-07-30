/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TelemetryMappers - Applies "Live - Telemetrie" DTOs onto existing
 ## Entities. Same rules as ChampionshipMappers.cs: TryApply mutates
 ## "target" and never creates one, returns null on success or a DataError
 ## (kind Validation) when the identifying field is missing/inconsistent.
 ##
 */

// SpeedSample - keyed by timestamp (one entry of a sample history).
public static class SpeedSampleMapper
{
    public static DataError? TryApply(this SpeedSampleDTO dto, SpeedSample target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.timestamp))
            return new DataError(DataErrorKind.Validation, "SpeedSampleMapper", "speed sample missing timestamp");

        target.speed = dto.speed;
        target.gear = dto.gear;
        target.timestamp = dto.timestamp;
        return null;
    }
}

// EngineSample - keyed by timestamp (one entry of a sample history).
public static class EngineSampleMapper
{
    public static DataError? TryApply(this EngineSampleDTO dto, EngineSample target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.timestamp))
            return new DataError(DataErrorKind.Validation, "EngineSampleMapper", "engine sample missing timestamp");

        target.rpm = dto.rpm;
        target.gear = dto.gear;
        target.throttlePercent = dto.throttlePercent;
        target.brakePercent = dto.brakePercent;
        target.drsActive = dto.drsActive;
        target.timestamp = dto.timestamp;
        return null;
    }
}

// LocationSample - keyed by timestamp.
public static class LocationSampleMapper
{
    public static DataError? TryApply(this LocationSampleDTO dto, LocationSample target)
    {
        if (dto == null || string.IsNullOrEmpty(dto.timestamp))
            return new DataError(DataErrorKind.Validation, "LocationSampleMapper", "location sample missing timestamp");

        target.x = dto.x;
        target.y = dto.y;
        target.z = dto.z;
        target.timestamp = dto.timestamp;
        return null;
    }
}

// DriverIntervals
public static class DriverIntervalsMapper
{
    public static DataError? TryApply(this IntervalsDTO dto, DriverIntervals target)
    {
        if (dto == null || dto.driverNumber <= 0)
            return new DataError(DataErrorKind.Validation, "DriverIntervalsMapper", "intervals missing driverNumber");

        target.driverNumber = dto.driverNumber;
        target.gapToLeader = dto.gapToLeader;
        target.gapAhead = dto.gapAhead;
        target.timestamp = dto.timestamp;
        return null;
    }
}
