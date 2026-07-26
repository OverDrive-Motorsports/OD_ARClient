/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## TeamRadioDTO - Mirrors GET /sessions/{id}/race/radio (one entry).
 ##
 */

using System;

[Serializable]
public class TeamRadioMessageDTO
{
    public int driverNumber;
    public string timestamp;
    public string audioUrl;
}
