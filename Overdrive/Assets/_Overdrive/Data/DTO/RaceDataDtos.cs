/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceDataDtos - Mirrors of race-data-service JSON (gateway prefix
 ## /v1/race-data). camelCase keys, bare arrays, no envelope.
 ## Source of truth: services/race-data-service/src/core/domain/race_live.entity.go
 ##
 */

using System;
using System.Collections.Generic;

/// <summary>GET /sessions/{id}/race/position — latest sample per driver, unordered.</summary>
public class RacePositionDto
{
    public int driverNumber;
    public int position;
    public string gapToLeader;
    public string gapAhead;
    public string gapBehind;
    public int lapsCompleted;
    public DateTime timestamp;
}

public class RaceLapEntryDto
{
    public int lapNumber;
    public float? lapDuration;
    public float? sector1;
    public float? sector2;
    public float? sector3;
    public bool isPitOutLap;
}

/// <summary>GET /sessions/{id}/race/laps — one object per driver.</summary>
public class RaceLapsDto
{
    public int driverNumber;
    public List<RaceLapEntryDto> laps;
    public float? bestLap;
    public float? averageLap;
}

/// <summary>GET /sessions/{id}/race/stints</summary>
public class RaceStintDto
{
    public int driverNumber;
    public int stintNumber;
    public string compound;
    public int lapStart;
    public int lapEnd;
    public int tyreAgeAtStart;
}

/// <summary>GET /sessions/{id}/race/pitstops</summary>
public class RacePitStopDto
{
    public int driverNumber;
    public int lapNumber;
    public float pitDuration;
}

/// <summary>GET /sessions/{id}/race/weather — every sample of the session.</summary>
public class RaceWeatherSampleDto
{
    public DateTime timestamp;
    public float? airTemperature;
    public float? trackTemperature;
    public float? humidity;
    /// <summary>OpenF1 wind speed, in m/s.</summary>
    public float? windSpeed;
    public bool rainfall;
}
