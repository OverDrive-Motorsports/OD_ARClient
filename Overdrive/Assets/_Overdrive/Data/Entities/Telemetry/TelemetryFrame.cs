/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TelemetryFrame - Everything the telemetry widgets show, as one frame of
 ## the feed:
 ##   • car — CarTelemetry, the live sample of the followed car (speed,
 ##     pedals, where it is on the lap);
 ##   • trace — LapTrace, its last lap sampled along the distance;
 ##   • timing / rival — LapTiming of the followed car and of the compared one.
 ## Filled from the backend telemetry by ODTelemetryMapper (made up by
 ## ODMockTelemetry for the catalogue previews). Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;

[Serializable]
public class TelemetryFrame
{
    public CarTelemetry car;
    public LapTrace trace = new();
    public LapTiming timing = new();
    public LapTiming rival = new();
}

[Serializable]
public struct CarTelemetry
{
    public float speedKmh;      // 0 – ~350
    public int gear;            // 0 = neutral, 1 – 8
    public float rpm;           // engine speed
    public float maxRpm;        // red line
    public float throttle;      // 0 – 1
    public float brake;         // 0 – 1
    public float lapDistance;   // 0 – 1, share of the lap already driven
    public string location;     // "Turn 13", "Straight"
}

[Serializable]
public class LapTrace
{
    public float lapLengthKm;                           // 5.4
    public float[] sectorEnds = Array.Empty<float>();   // share of the lap where S1, S2... end
    // One value per slice of track, from the start line: the current lap up to the car,
    // the previous lap after it.
    public float[] speed = Array.Empty<float>();        // km/h
    public float[] throttle = Array.Empty<float>();     // 0 – 1
    public float[] brake = Array.Empty<float>();        // 0 – 1

    public int Bins => speed.Length;
}
