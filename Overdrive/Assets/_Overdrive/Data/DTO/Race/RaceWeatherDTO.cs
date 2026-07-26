/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceWeatherDTO - Mirrors GET /sessions/{id}/race/weather (one entry of
 ## the time series). Session metadata (Championship/SessionDTO) has no
 ## weather field at all - this is the only weather data in the project.
 ##
 */

using System;

[Serializable]
public class RaceWeatherSampleDTO
{
    public string timestamp;
    public float airTemperature;
    public float trackTemperature;
    public int humidity;
    public float windSpeed;
    public bool rainfall;
}
