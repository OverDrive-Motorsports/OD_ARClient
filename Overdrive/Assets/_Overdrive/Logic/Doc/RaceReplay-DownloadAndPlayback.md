# Replays: catalogue, download and playback

A replay is **downloaded to the headset**, then played from the local file,
online or offline. There is only one playback path.

## 1. Catalogue — done

`GET /v1/race-data/championships/{code}/replays` lists the replayable
sessions: the ones for which the backend stored the telemetry **and** the
x/y/z positions. All session types, newest first.

```
ODReplayCatalog → ODReplayService.LoadCatalogAsync()
                    ├── ODBackendApi.GetChampionships()   (championship name)
                    └── ODBackendApi.GetReplays(code)     → ReplayCatalogEntryDto
                  → ODChampionshipMapper.ToReplaySummary() → ReplaySummary
```

Two requests in parallel, whatever the number of events.

## 2. Download — done (not wired into the UI yet)

`GET /v1/race-data/sessions/{sessionId}/race/replay` returns the whole
session as one JSON, uncompressed and without `Content-Length`:

| Key | Shape | Content |
|---|---|---|
| `telemetry` | per driver (`"44": [...]`) | speed, rpm, gear, throttle, brake, DRS, lap, **x/y/z**, timestamp |
| `position` | per driver | position, gaps, completed laps, timestamp |
| `radio` | per driver | audio URL, timestamp |
| `lap` | per driver | lap time and sectors — **no timestamp** |
| `stint` | per driver | tyres, first and last lap — **no timestamp** |
| `pitStop` | per driver | lap, duration — **no timestamp** |
| `raceControl` | flat list | flags, Safety Car, messages, timestamp |
| `weather` | flat list | temperatures, wind, rain, timestamp |

```
ODReplayService.DownloadAsync(summary, progress, token)
  ├── ODReplayStore.BeginDownload()             empty folder, returns replay.json.part
  ├── ODBackendApi.DownloadReplay()             writes the body straight to disk
  ├── GetSessionResult + GetSessionDrivers      the podium, to show it offline
  └── ODReplayStore.Commit()                    .part → replay.json, then meta.json last
```

- **Memory**: `DownloadHandlerFile` streams to disk; the JSON is never
  loaded in RAM during the download.
- **Progress**: in bytes received (the backend doesn't send the total size).
- **Timeout**: no maximum duration, but the download gives up after
  `ODApiConfig.downloadStallSeconds` without receiving data.
- **Failure or cancellation**: the folder is deleted, nothing is left.
- **One download per session**: asking for the same one again returns the running task.
- Once downloaded, `ODReplayService.LoadDetailAsync` reads the podium
  from `meta.json` instead of calling the backend.

Storage: see [Data/Doc/README.md](../../Data/Doc/README.md#downloaded-replays).

## 3. Playback — basic version done (no controls)

When the page opens, the replay is downloaded if needed, then read; the
cars wait on the grid ("Ready · press "Watch replay"").
The race starts from lights out, in real time, when "Watch replay" is
pressed (capsule or map screen, which also switch the TV on), and stops
when the page is left (back to the gallery, page hidden). A press during
the download starts the race as soon as it is ready.

```
ODSportPage.LoadReplay(summary)
  ├── ODReplayService.LoadDetailAsync()       podium (meta.json if downloaded)
  ├── ODReplayService.DownloadAsync()         only if it isn't on the headset
  ├── ODReplayService.LoadRaceAsync()         Task.Run → ODRaceReplayReader.Read()
  └── ODReplayPlayer.Load(race, summary, ODReplayTrackMap)   grid, clock stopped
ODChampionshipPanel.ReplayWatchPressed → ODSportPage.WatchReplay()
  └── ODReplayPlayer.Begin()
        ├── every frame      ODReplayMapper.TryGetPosition() → ODReplayTrackMap.MoveCar()
        ├── every 0.5 s      ODReplayMapper.ToFrame() → OrderChanged (if changed)
        │                      → RaceRankingManager.ShowEntries + podium
        └── every second     ProgressChanged → "Lap 12 / 56 · 0:21:45"
```

- **`ODRaceReplayReader`** streams `replay.json` (`JsonTextReader`):
  telemetry by hand (only time, x, y and speed are kept), the rest through
  `JObject`. The China 2025 replay (108 MB) reads in ~3 s on a Mac and
  takes ~12 MB (odometer included).
- **Clock**: seconds since UTC midnight of the first timestamp.
  Start = race control's `SESSION STARTED` message.
- **Laps**: the telemetry `lapNumber` is the leader's for every car, so it
  is not used. A car's lap n starts at = start + the sum of its lap times
  (≈ 1 s off at the flag).
- **Standings**: last official position. The backend gives no gaps
  (`gapAhead` / `gapBehind` always null): the gap to the leader is computed
  where the car is, = how long ago the leader was at the same point of the
  race (`ODReplayMapper.Progress` / `TimeAt`: each lap split in three at the
  sector lines, steady pace within a sector). So it is exact at every sector
  line and moves continuously in between (refreshed every 0.5 s). "+n laps"
  when the leader is n laps ahead; "Retired" when a car's laps stop more
  than 2 min before the chequered flag.
- **Columns**: tyre = Pirelli-coloured badge (S red, M yellow, H white,
  I green, W blue) + age in laps; stops; gap; last lap (purple = fastest
  lap).
- **Map**: the outline is the winner's fastest lap outside the pits
  (`ODTrackLine`), one dot per car in its team colour, interpolated between
  two samples (hidden when the samples are more than 3 s apart).

**Backend bug**: `/race/replay` gives every car the same x/y (and the same
`lapNumber`): `ListTelemetryFrames` matches each sample with the closest
position *across all drivers*. The reader detects it
(`RaceReplay.sharedPositions`: most cars have exactly the first car's
samples) and then places each car on the outline from **its own speed**,
which is correct:
- **Odometer** (`ReplayCar.odometer`): metres travelled, integrated from
  the speed. Within a lap, the car moves by the metres travelled between its
  two line crossings: slow in the corners, fast on the straights. A lap is
  ~5,375 m in Shanghai, within ±10 m from one lap to the next.
- **Grid**: lap 1 is counted backwards from the first line crossing. Each
  car therefore starts from its real slot, which can be past the line (in
  Shanghai, the grid is 125 to 170 m past the timing line; pit-lane starts
  are visible).
- **Timing line** (`RaceReplay.lineOffset`): the shared x/y follow one car
  at a time (and sometimes switch). At each sample, the place a on the
  outline matches the lap fraction f of one of the cars. The peak of a − f
  over the whole race gives the line (19 % of the outline for China 2025).
  So the lap changes at the right place.

Pit stops don't show on the map. Once the backend is fixed (and the replay
downloaded again), the real positions are used automatically.

**Testing**: in the Editor, in Play mode, the Sport page object has an
`ODReplayPlayer` whose `speed` field (race seconds per second) can be raised
to 60 to run through the race in a minute.

Next steps:
1. **Controls**: play/pause, speed, seek (the clock is
   `ODReplayPlayer.RaceTime`, the data is found by binary search).
2. **Conversion** to a binary `replay.odr` at the end of the download, to
   open the replay without parsing 100 MB of JSON again (bump
   `ODReplayStore.FormatVersion`).
3. **Display**: flags / Safety Car (race control), telemetry of the selected
   driver, cars on the 3D circuit (`CircuitPreview`).

## 4. Gallery — to do

State per replay (Download → progress → Downloaded → Delete),
"My replays" = downloaded replays (`ODReplayService.Downloaded()`),
and, without network, only the downloaded replays.
