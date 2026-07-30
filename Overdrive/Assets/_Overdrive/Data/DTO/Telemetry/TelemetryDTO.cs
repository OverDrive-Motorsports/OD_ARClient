/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TelemetryDTO - Mirrors the "Live - Telemetrie" section, one driver at a
 ## time (path already scopes sessionId + driverNumber): GET .../telemetry/speed,
 ## /telemetry/engine, /telemetry/location, /telemetry/intervals. speed/engine
 ## /location are full sample histories (arrays), intervals is the latest
 ## known sample only (single object). No driverNumber field on any of these -
 ## the path already scopes it.
 ##
 ## battery does NOT exist on /telemetry/engine (confirmed: no data source) -
 ## not modeled, do not add it back without a real source.
 ##
 */

using System;

// GET .../telemetry/speed (one entry of the array)
[Serializable]
public class SpeedSampleDTO
{
    public int speed;
    public int gear;
    public string timestamp;
}

// GET .../telemetry/engine (one entry of the array)
[Serializable]
public class EngineSampleDTO
{
    public int rpm;
    public int gear;
    public int throttlePercent;
    public int brakePercent;
    public bool drsActive;
    public string timestamp;
}

// GET .../telemetry/location (one entry of the array)
[Serializable]
public class LocationSampleDTO
{
    public float x;
    public float y;
    public float z;
    public string timestamp;
}

// GET .../telemetry/intervals - single object, latest known sample, no lapNumber filter.
[Serializable]
public class IntervalsDTO
{
    public int driverNumber;
    public string gapToLeader;
    public string gapAhead; // null for the race leader
    public string timestamp;
}
