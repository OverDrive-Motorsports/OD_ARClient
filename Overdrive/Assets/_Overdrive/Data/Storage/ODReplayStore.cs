/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayStore - Replays downloaded on the headset, one folder each under
 ## persistentDataPath/replays/: the backend replay data plus a meta.json
 ## (DownloadedReplayDto). A folder only counts once its meta.json exists,
 ## and meta.json is written last, so an interrupted download never shows
 ## up as a replay. Files only: the download itself is ODReplayService's.
 ##
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class ODReplayStore
{
    /// <summary>Bump when the saved files change shape. 1: raw backend JSON of /race/replay.</summary>
    public const int FormatVersion = 1;

    const string DataFile = "replay.json";
    const string MetaFile = "meta.json";
    const string PartSuffix = ".part";

    // RoundtripKind: ReplaySummary.date is local time and must come back as local time.
    static readonly JsonSerializerSettings Json = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
    };

    static string _root;
    static Dictionary<string, DownloadedReplayDto> _index;

    /// <summary>Raised on the main thread after a replay is saved or deleted.</summary>
    public static event Action Changed;

    /// <summary>Folder holding one sub-folder per replay. Tests point it at a temporary folder.</summary>
    public static string Root
    {
        get => _root ??= Path.Combine(Application.persistentDataPath, "replays");
        set { _root = value; _index = null; }
    }

    public static IReadOnlyCollection<DownloadedReplayDto> All => Index.Values;

    /// <summary>The downloaded replay, or null.</summary>
    public static DownloadedReplayDto Get(string sessionId) =>
        sessionId != null && Index.TryGetValue(sessionId, out var replay) ? replay : null;

    /// <summary>False for a replay saved by an older app version: it has to be downloaded again.</summary>
    public static bool IsCurrent(DownloadedReplayDto replay) => replay != null && replay.formatVersion == FormatVersion;

    public static long TotalBytes => Index.Values.Sum(r => r.sizeBytes);

    public static string DataPath(string sessionId) => Path.Combine(Folder(sessionId), DataFile);

    /// <summary>Clears any earlier copy and returns the temporary file to download into.</summary>
    public static string BeginDownload(string sessionId)
    {
        Delete(sessionId);
        Directory.CreateDirectory(Folder(sessionId));
        return DataPath(sessionId) + PartSuffix;
    }

    /// <summary>Promotes the finished download and writes meta.json, which makes it a replay.</summary>
    public static DownloadedReplayDto Commit(ReplaySummary summary, List<SessionResultRowDto> results, List<DriverSummaryDto> drivers)
    {
        string data = DataPath(summary.id);
        if (File.Exists(data)) File.Delete(data);
        File.Move(data + PartSuffix, data);

        var replay = new DownloadedReplayDto
        {
            formatVersion = FormatVersion,
            downloadedAt = DateTime.UtcNow,
            sizeBytes = new FileInfo(data).Length,
            summary = summary,
            results = results,
            drivers = drivers,
        };
        string meta = Path.Combine(Folder(summary.id), MetaFile);
        File.WriteAllText(meta + PartSuffix, JsonConvert.SerializeObject(replay, Json));
        if (File.Exists(meta)) File.Delete(meta);
        File.Move(meta + PartSuffix, meta);

        Index[summary.id] = replay;
        Changed?.Invoke();
        return replay;
    }

    /// <summary>Removes the replay, or what an unfinished download left behind.</summary>
    public static void Delete(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;
        string folder = Folder(sessionId);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        if (Index.Remove(sessionId)) Changed?.Invoke();
    }

    static Dictionary<string, DownloadedReplayDto> Index => _index ??= Scan();

    /// <summary>Reads every meta.json; folders without a readable one are leftovers and get deleted.</summary>
    static Dictionary<string, DownloadedReplayDto> Scan()
    {
        var index = new Dictionary<string, DownloadedReplayDto>();
        if (!Directory.Exists(Root)) return index;

        foreach (string folder in Directory.GetDirectories(Root))
        {
            var replay = Read(folder);
            if (replay != null && !index.ContainsKey(replay.summary.id))
                index[replay.summary.id] = replay;
            else
                TryDelete(folder);
        }
        return index;
    }

    static DownloadedReplayDto Read(string folder)
    {
        try
        {
            string meta = Path.Combine(folder, MetaFile);
            if (!File.Exists(meta) || !File.Exists(Path.Combine(folder, DataFile))) return null;
            var replay = JsonConvert.DeserializeObject<DownloadedReplayDto>(File.ReadAllText(meta), Json);
            return string.IsNullOrEmpty(replay?.summary.id) ? null : replay;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ODReplayStore] Unreadable replay in {folder}: {e.Message}");
            return null;
        }
    }

    static void TryDelete(string folder)
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ODReplayStore] Could not remove {folder}: {e.Message}");
        }
    }

    static string Folder(string sessionId) => Path.Combine(Root, SafeName(sessionId));

    /// <summary>"openf1:session:9472" → "openf1_session_9472": no ':' (Windows) and no '.' or '/' to leave Root.</summary>
    static string SafeName(string sessionId) =>
        new string(sessionId.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
}
