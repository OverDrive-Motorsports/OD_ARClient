/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayService - Replays seen from the app: the backend catalogue of
 ## playable sessions, the podium of one replay, downloading a replay to
 ## the headset (ODReplayStore) so it can be opened without network, and
 ## loading it for playback (RaceReplay).
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class ODReplayService
{
    static string Code => ODApiConfig.Instance.championshipCode;

    static readonly Dictionary<string, Task<DownloadedReplayDto>> Downloads = new Dictionary<string, Task<DownloadedReplayDto>>();

    /// <summary>Sessions the backend can replay (telemetry and car locations stored), most recent first.</summary>
    public static async Task<List<ReplaySummary>> LoadCatalogAsync()
    {
        var championshipsTask = ODBackendApi.GetChampionships();
        var entriesTask = ODBackendApi.GetReplays(Code);
        await Task.WhenAll(championshipsTask, entriesTask);

        var championship = championshipsTask.Result?.FirstOrDefault(c => c.championshipCode == Code);
        return (entriesTask.Result ?? new List<ReplayCatalogEntryDto>())
            .Where(e => !string.IsNullOrEmpty(e?.sessionId))
            .Select(e => ODChampionshipMapper.ToReplaySummary(championship, Code, e))
            .OrderByDescending(r => r.date)
            .ToList();
    }

    /// <summary>Replays saved on the headset, most recent session first.</summary>
    public static List<ReplaySummary> Downloaded() =>
        ODReplayStore.All.Select(r => r.summary).OrderByDescending(r => r.date).ToList();

    public static bool IsDownloaded(string sessionId) => ODReplayStore.IsCurrent(ODReplayStore.Get(sessionId));

    public static bool IsDownloading(string sessionId) => sessionId != null && Downloads.ContainsKey(sessionId);

    /// <summary>Podium of the replay — read from the headset when it was downloaded, from the backend otherwise.</summary>
    public static async Task<ReplayDetail> LoadDetailAsync(ReplaySummary summary)
    {
        var saved = ODReplayStore.Get(summary.id);
        if (saved != null) return ToDetail(summary, saved.results, saved.drivers);

        var resultTask = ODBackendApi.GetSessionResult(summary.id);
        var driversTask = ODBackendApi.GetSessionDrivers(summary.id);
        await Task.WhenAll(resultTask, driversTask);
        return ToDetail(summary, resultTask.Result?.data, driversTask.Result);
    }

    /// <summary>
    /// Downloads the replay data and its podium to the headset. Asking again while it runs returns
    /// the same download. <paramref name="progress"/> receives the bytes received so far. Nothing is
    /// left on disk when it fails or is cancelled.
    /// </summary>
    public static Task<DownloadedReplayDto> DownloadAsync(ReplaySummary summary, IProgress<ulong> progress = null,
        CancellationToken token = default)
    {
        if (Downloads.TryGetValue(summary.id, out var running)) return running;

        var task = Download(summary, progress, token);
        if (!task.IsCompleted) Downloads[summary.id] = task;
        return task;
    }

    /// <summary>
    /// The replay ready to play: downloaded first when it is not on the headset yet
    /// (<paramref name="progress"/> receives the bytes received), then read off the main thread.
    /// </summary>
    public static async Task<RaceReplay> LoadRaceAsync(ReplaySummary summary, IProgress<ulong> progress = null,
        CancellationToken token = default)
    {
        if (!IsDownloaded(summary.id)) await DownloadAsync(summary, progress, token);
        string path = ODReplayStore.DataPath(summary.id);
        var replay = await Task.Run(() => ODRaceReplayReader.Read(path), token);
        replay.drivers = ODChampionshipMapper.DriverLookup(ODReplayStore.Get(summary.id)?.drivers);
        return replay;
    }

    static async Task<DownloadedReplayDto> Download(ReplaySummary summary, IProgress<ulong> progress, CancellationToken token)
    {
        try
        {
            string part = ODReplayStore.BeginDownload(summary.id);
            var resultTask = ODBackendApi.GetSessionResult(summary.id);
            var driversTask = ODBackendApi.GetSessionDrivers(summary.id);
            await ODBackendApi.DownloadReplay(summary.id, part, progress, token);
            await Task.WhenAll(resultTask, driversTask);
            return ODReplayStore.Commit(summary, resultTask.Result?.data, driversTask.Result);
        }
        catch
        {
            ODReplayStore.Delete(summary.id);
            throw;
        }
        finally
        {
            Downloads.Remove(summary.id);
        }
    }

    static ReplayDetail ToDetail(ReplaySummary summary, List<SessionResultRowDto> results, List<DriverSummaryDto> drivers) =>
        ODChampionshipMapper.ToReplayDetail(summary, null, results, ODChampionshipMapper.DriverLookup(drivers));
}
