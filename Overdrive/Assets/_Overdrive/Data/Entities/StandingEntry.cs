/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## StandingEntry - One driver's row in a session's standings. Re-fetched on
 ## a timer while a session is live and updated in place each time (see
 ## Mappers/EntityCollectionSync.cs).
 ##
 */

public class StandingEntry
{
    public int position;
    public int driverNumber;
    public string teamId;
    public string gapToLeader;
    public int points;
}
