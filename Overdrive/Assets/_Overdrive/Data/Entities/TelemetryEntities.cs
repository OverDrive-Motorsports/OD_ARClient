/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TelemetryEntities - "Live - Telemetrie" domain, one driver at a time.
 ## Refreshed often (highest frequency in the app) - mutated in place by
 ## Mappers/TelemetryMappers.cs. SpeedSample/EngineSample/LocationSample are
 ## full sample histories (lists, keyed by timestamp); DriverIntervals is
 ## the one single-object entity here (latest known sample only).
 ##
 */

// One point of a driver's speed/gear sample history.
public class SpeedSample
{
    public int speed;
    public int gear;
    public string timestamp;
}

// One point of a driver's engine sample history. No battery field - confirmed no data source.
public class EngineSample
{
    public int rpm;
    public int gear;
    public int throttlePercent;
    public int brakePercent;
    public bool drsActive;
    public string timestamp;
}

// One point of a driver's on-track position time series.
public class LocationSample
{
    public float x;
    public float y;
    public float z;
    public string timestamp;
}

// DriverIntervals - one driver's live gap to the car ahead/leader (latest known sample).
public class DriverIntervals
{
    public int driverNumber;
    public string gapToLeader;
    public string gapAhead;
    public string timestamp;
}
