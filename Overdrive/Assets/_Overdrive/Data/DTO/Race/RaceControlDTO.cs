/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceControlDTO - Mirrors POST /sessions/{id}/race/control (flags, safety
 ## car, penalties). Unlike every other endpoint in this project this one is
 ## a POST that responds once an event happens (long-poll style - "relancer
 ## un post apres" per the source doc), not a plain GET - Core/Network will
 ## need a different call pattern for this later, not ApiClient.Get.
 ##
 ## penalty.driverNumber/timePenalty types are a best guess: the source doc
 ## shows placeholder values ("XX") instead of real examples for these two
 ## fields - confirm with backend before relying on them.
 ##
 */

using System;

[Serializable]
public class PenaltyDTO
{
    public int driverNumber;
    public float timePenalty;
}

// One entry of the race/control response array
[Serializable]
public class RaceControlEventDTO
{
    public string category;
    public string flag;
    public PenaltyDTO penality; // sic - matches the field name the backend actually sends
    public string safetyCar;    // "SC", "VSC", or null
    public string message;
    public int lapNumber;
    public string timestamp;
}
