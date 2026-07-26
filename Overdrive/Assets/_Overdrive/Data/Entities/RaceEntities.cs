/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceEntities - "Live - Course" domain: position, laps, stints, pitstops,
 ## race control, weather, team radio. All refreshed often while a session
 ## is live - mutated in place by Mappers/RaceMappers.cs.
 ##
 */

using System.Collections.Generic;

// RacePosition - one driver's live position/gaps.
public class RacePosition
{
    public int driverNumber;
    public int position;
    public string gapToLeader;
    public string gapAhead;
    public string gapBehind;
    public int lapsCompleted;
    public string timestamp;
}

// Nested data, not fetched on its own - always attached to a DriverLaps.
public class Lap
{
    public int lapNumber;
    public float lapDuration;
    public float sector1;
    public float sector2;
    public float sector3;
    public bool isPitOutLap;
}

// DriverLaps - lap times/sectors for one driver.
public class DriverLaps
{
    public int driverNumber;
    public List<Lap> laps = new List<Lap>();
    public float bestLap;
    public float averageLap;
}

// Stint - one tyre stint for one driver.
public class Stint
{
    public int driverNumber;
    public int stintNumber;
    public string compound;
    public int lapStart;
    public int lapEnd;
    public int tyreAgeAtStart;
}

// PitStop - one pit stop for one driver.
public class PitStop
{
    public int driverNumber;
    public int lapNumber;
    public float pitDuration;
}

// Nested data, not fetched on its own - always attached to a RaceControlEvent (when there's a penalty).
public class Penalty
{
    public int driverNumber;
    public float timePenalty;
}

// RaceControlEvent - one flag/safety-car/penalty event.
public class RaceControlEvent
{
    public string category;
    public string flag;
    public Penalty penalty;
    public string safetyCar;
    public string message;
    public int lapNumber;
    public string timestamp;
}

// WeatherSample - one point of the race weather time series.
public class WeatherSample
{
    public string timestamp;
    public float airTemperature;
    public float trackTemperature;
    public int humidity;
    public float windSpeed;
    public bool rainfall;
}

// TeamRadioMessage - one team radio clip.
public class TeamRadioMessage
{
    public int driverNumber;
    public string timestamp;
    public string audioUrl;
}
