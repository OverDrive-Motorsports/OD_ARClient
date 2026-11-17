# Data

Represents the application's data. Bridges the backend (JSON) and objects
usable on the Unity side. Purely data-oriented layer: no business logic, no
dependency on UI or Interaction.

## Structure

```
Data/
├── DTO/       Mirrors of the backend JSON (ChampionshipDtos, RaceDataDtos) and of the files saved on the headset (ReplayStoreDtos)
├── Entities/  What the screens display (OffSeasonData, WeekendData, LiveSessionData, ReplayData, SeasonStandings...) and RaceReplay (a replay ready to play)
├── Mappers/   ODChampionshipMapper — pure DTO -> Entity functions; ODReplayMapper — a RaceReplay at a given time
└── Storage/   ODReplayStore — replays downloaded on the headset; ODRaceReplayReader — replay.json -> RaceReplay
```

- Every DTO name ends with `Dto` so `DTO/link.xml` keeps their fields from
  IL2CPP stripping (Newtonsoft only reaches them through reflection).
- `Data/` has its own `Overdrive.Data.asmdef`, so the EditMode tests
  (`Tests/EditMode`) can reference it: a test assembly can't see
  `Assembly-CSharp`.

## Downloaded replays

`ODReplayStore` keeps one folder per replay under
`persistentDataPath/replays/`:

```
replays/openf1_session_9472/
├── replay.json   The backend's /race/replay body, as received
└── meta.json     DownloadedReplayDto: summary, podium rows, drivers, format version, size
```

The data is downloaded into `replay.json.part`, and `meta.json` is written
last: a folder without it is an interrupted download and is deleted on the
next scan. `FormatVersion` is bumped whenever these files change shape, so
older downloads show up as "download again".

## Allowed dependencies

None. Data doesn't reference any other layer of the project.
