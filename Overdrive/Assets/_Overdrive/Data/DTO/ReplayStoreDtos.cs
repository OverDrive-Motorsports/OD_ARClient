/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ReplayStoreDtos - meta.json saved next to each replay downloaded on the
 ## headset (ODReplayStore). It keeps the backend rows as received, so the
 ## replay can be listed and its podium shown without any network.
 ##
 */

using System;
using System.Collections.Generic;

public class DownloadedReplayDto
{
    /// <summary>ODReplayStore.FormatVersion at download time: an older one means "download again".</summary>
    public int formatVersion;
    public DateTime downloadedAt;
    /// <summary>Size of the replay data file, in bytes.</summary>
    public long sizeBytes;
    public ReplaySummary summary;
    /// <summary>session_result dataset rows, for the podium.</summary>
    public List<SessionResultRowDto> results;
    public List<DriverSummaryDto> drivers;
}
