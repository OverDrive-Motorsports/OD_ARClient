/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODRaceReplayReader - Turns replay.json (the backend /race/replay body
 ## saved by ODReplayStore) into a RaceReplay. The file is read as a stream:
 ## the 100 MB of JSON never sit in memory, only the arrays that are kept.
 ## Blocking: call it off the main thread.
 ##
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class ODRaceReplayReader
{
    public static RaceReplay Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        using var text = new StreamReader(stream);
        return Read(text);
    }

    public static RaceReplay Read(TextReader text)
    {
        using var reader = new JsonTextReader(text)
        {
            DateParseHandling = DateParseHandling.None,     // timestamps stay strings: ParseUtc is much faster
            PropertyNameTable = new DefaultJsonNameTable(),  // one string per field name, not per row
        };
        var file = new Parsed();
        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            throw new InvalidDataException("replay.json is not a JSON object.");

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            string section = (string)reader.Value;
            reader.Read();
            switch (section)
            {
                case "telemetry": ForEachCar(reader, file, ReadSamples); break;
                case "position":
                    ForEachCar(reader, file, (r, car) => ForEachRow(r, row =>
                    {
                        if (row.Value<int?>("position") is int position && file.Seconds(row.Value<string>("timestamp")) is float t)
                            car.positions.Add((t, position));
                    }));
                    break;
                case "lap":
                    ForEachCar(reader, file, (r, car) => ForEachRow(r, row =>
                    {
                        if (row.Value<int?>("lapNumber") is int lap && lap > 0)
                            car.laps.Add((lap, row.Value<float?>("lapDuration") ?? float.NaN,
                                row.Value<float?>("sector1") ?? float.NaN, row.Value<float?>("sector2") ?? float.NaN));
                    }));
                    break;
                case "stint":
                    ForEachCar(reader, file, (r, car) => ForEachRow(r, row => car.stints.Add(new ReplayStint
                    {
                        compound = row.Value<string>("compound"),
                        lapStart = row.Value<int?>("lapStart") ?? 0,
                        lapEnd = row.Value<int?>("lapEnd") ?? 0,
                        tyreAgeAtStart = row.Value<int?>("tyreAgeAtStart") ?? 0,
                    })));
                    break;
                case "pitStop":
                    ForEachCar(reader, file, (r, car) => ForEachRow(r, row =>
                    {
                        if (row.Value<int?>("lapNumber") is int lap) car.pits.Add(lap);
                    }));
                    break;
                case "raceControl": ForEachRow(reader, file.ReadRaceControl); break;
                default: reader.Skip(); break;   // radio, weather: not played yet
            }
        }
        return file.Build();
    }

    // ── JSON walking ────────────────────────────────────────────────────────

    /// <summary>{"44": [...], "1": [...]}: the reader stands on the object's start.</summary>
    static void ForEachCar(JsonReader reader, Parsed file, Action<JsonReader, CarRows> read)
    {
        if (reader.TokenType != JsonToken.StartObject) { reader.Skip(); return; }
        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            bool known = int.TryParse((string)reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number);
            reader.Read();
            if (known) read(reader, file.Car(number));
            else reader.Skip();
        }
    }

    /// <summary>[{...}, {...}]: calls <paramref name="read"/> on each object's start; it must consume the object.</summary>
    static void ForEachObject(JsonReader reader, Action read)
    {
        if (reader.TokenType != JsonToken.StartArray) { reader.Skip(); return; }
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            if (reader.TokenType == JsonToken.StartObject) read();
            else reader.Skip();
        }
    }

    static void ForEachRow(JsonReader reader, Action<JObject> read) => ForEachObject(reader, () => read(JObject.Load(reader)));

    /// <summary>Telemetry rows, by hand: they are 95 % of the file. Only time, x/y and speed are kept.</summary>
    static void ReadSamples(JsonReader reader, CarRows car) => ForEachObject(reader, () =>
    {
        string timestamp = null;
        float x = 0f, y = 0f, speed = float.NaN;
        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            string name = (string)reader.Value;
            reader.Read();
            switch (name)
            {
                case "timestamp": timestamp = reader.Value as string; break;
                case "x": x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture); break;
                case "y": y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture); break;
                case "speed": if (reader.Value != null) speed = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture); break;
                default: reader.Skip(); break;
            }
        }
        if (!(car.file.Seconds(timestamp) is float t)) return;
        if (!float.IsNaN(speed))
        {
            car.speedTime.Add(t);
            car.speed.Add(speed);
        }
        // (0, 0) is "no position" (car in the garage before the session).
        if (x != 0f || y != 0f)
        {
            car.time.Add(t);
            car.x.Add(x);
            car.y.Add(y);
        }
    });

    /// <summary>"2025-03-23T06:09:12.528Z", the backend's format, parsed by hand; anything else by DateTime.Parse.</summary>
    public static DateTime ParseUtc(string s)
    {
        if (s.Length >= 20 && s[4] == '-' && s[7] == '-' && s[10] == 'T' && s[13] == ':' && s[16] == ':' && s[s.Length - 1] == 'Z')
        {
            long ticks = new DateTime(Digits(s, 0, 4), Digits(s, 5, 2), Digits(s, 8, 2),
                Digits(s, 11, 2), Digits(s, 14, 2), Digits(s, 17, 2), DateTimeKind.Utc).Ticks;
            if (s[19] == '.')
            {
                int digits = 0;
                long fraction = 0;
                for (int i = 20; i < s.Length - 1 && digits < 7; i++, digits++) fraction = fraction * 10 + (s[i] - '0');
                for (; digits < 7; digits++) fraction *= 10;
                ticks += fraction;
            }
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    static int Digits(string s, int start, int count)
    {
        int value = 0;
        for (int i = start; i < start + count; i++) value = value * 10 + (s[i] - '0');
        return value;
    }

    // ── What is read, before it becomes a RaceReplay ────────────────────────

    sealed class CarRows
    {
        public Parsed file;
        public readonly List<float> time = new List<float>(), x = new List<float>(), y = new List<float>();
        public readonly List<float> speedTime = new List<float>(), speed = new List<float>();   // km/h
        public readonly List<(float time, int position)> positions = new List<(float, int)>();
        public readonly List<(int lap, float duration, float sector1, float sector2)> laps = new List<(int, float, float, float)>();
        public readonly List<ReplayStint> stints = new List<ReplayStint>();
        public readonly List<int> pits = new List<int>();
    }

    sealed class Parsed
    {
        readonly Dictionary<int, CarRows> _cars = new Dictionary<int, CarRows>();
        DateTime? _origin;
        float? _sessionStart, _chequered;

        public CarRows Car(int number)
        {
            if (!_cars.TryGetValue(number, out var car)) _cars[number] = car = new CarRows { file = this };
            return car;
        }

        /// <summary>Seconds from the midnight (UTC) of the first timestamp read.</summary>
        public float? Seconds(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp)) return null;
            DateTime utc = ParseUtc(timestamp);
            _origin ??= utc.Date;
            return (float)(utc - _origin.Value).TotalSeconds;
        }

        public void ReadRaceControl(JObject row)
        {
            string message = row.Value<string>("message") ?? "";
            if (_sessionStart == null && row.Value<string>("category") == "SessionStatus" && message.Contains("STARTED"))
                _sessionStart = Seconds(row.Value<string>("timestamp"));
            if (_chequered == null && row.Value<string>("flag") == "CHEQUERED")
                _chequered = Seconds(row.Value<string>("timestamp"));
        }

        public RaceReplay Build()
        {
            var withSamples = _cars.Values.Where(c => c.time.Count > 0).ToList();
            float firstSample = withSamples.Count > 0 ? withSamples.Min(c => c.time.Min()) : 0f;
            float lastSample = withSamples.Count > 0 ? withSamples.Max(c => c.time.Max()) : 0f;

            var replay = new RaceReplay
            {
                origin = _origin ?? default,
                start = _sessionStart ?? firstSample,
                end = lastSample,
                totalLaps = _cars.Values.SelectMany(c => c.laps).Select(l => l.lap).DefaultIfEmpty(0).Max(),
            };
            replay.finish = _chequered ?? replay.end;

            foreach (var pair in _cars)
                replay.cars[pair.Key] = BuildCar(pair.Key, pair.Value, replay.start);
            replay.end = Mathf.Max(replay.end, replay.cars.Values.Select(c => c.lapStart[c.lapCount + 1]).Where(t => !float.IsNaN(t)).DefaultIfEmpty(0f).Max());
            replay.outline = Outline(replay);
            replay.outlineDistance = Distances(replay.outline);
            replay.sharedPositions = SharePositions(replay.cars.Values.Where(c => c.time.Length > 0).ToList());
            if (replay.sharedPositions) replay.lineOffset = LineOffset(replay);
            return replay;
        }

        /// <summary>Cumulative distance along the closed outline (see RaceReplay.outlineDistance).</summary>
        static float[] Distances(Vector2[] outline)
        {
            if (outline.Length < 2) return Array.Empty<float>();
            var distance = new float[outline.Length + 1];
            for (int i = 1; i <= outline.Length; i++)
                distance[i] = distance[i - 1] + Vector2.Distance(outline[i - 1], outline[i % outline.Length]);
            return distance;
        }

        /// <summary>
        /// Where the timing line is on the outline. The shared x/y follow one car at a time
        /// (whichever it is), so at each sample one car's lap fraction f matches the sample's
        /// place a on the outline: a - f is the line's place. Over the race, the most frequent
        /// a - f wins (the wrong cars spread evenly).
        /// </summary>
        static float LineOffset(RaceReplay replay)
        {
            var stream = replay.cars.Values.FirstOrDefault(c => c.time.Length > 0);
            var cars = replay.cars.Values.Where(c => c.odometer.Length > 1 && !float.IsNaN(c.lapDistance)).ToList();
            var outline = replay.outline;
            float lap = replay.outlineDistance.Length > 1 ? replay.outlineDistance[replay.outlineDistance.Length - 1] : 0f;
            if (stream == null || cars.Count == 0 || outline.Length < 2 || lap <= 0f) return 0f;

            const int Bins = 200;
            var votes = new int[Bins];
            var offsets = new List<float>();
            int step = Mathf.Max(1, stream.time.Length / 600);
            for (int i = 0; i < stream.time.Length; i += step)
            {
                float t = stream.time[i];
                if (t < replay.start + 5f || t > replay.finish) continue;
                var p = new Vector2(stream.x[i], stream.y[i]);
                int nearest = 0;
                float best = float.MaxValue;
                for (int j = 0; j < outline.Length; j++)
                {
                    float d = (outline[j] - p).sqrMagnitude;
                    if (d < best) { best = d; nearest = j; }
                }
                float place = replay.outlineDistance[nearest] / lap;
                foreach (var car in cars)
                {
                    float offset = place - ODReplayMapper.LapFraction(car, t);
                    offset -= Mathf.Floor(offset);
                    votes[Mathf.Min(Bins - 1, (int)(offset * Bins))]++;
                    offsets.Add(offset);
                }
            }
            if (offsets.Count == 0) return 0f;

            int peak = Array.IndexOf(votes, votes.Max());
            float centre = (peak + .5f) / Bins, sum = 0f;
            int count = 0;
            foreach (float offset in offsets)
            {
                float delta = Mathf.Repeat(offset - centre + .5f, 1f) - .5f;   // circular
                if (Mathf.Abs(delta) <= 2.5f / Bins) { sum += delta; count++; }
            }
            return Mathf.Repeat(centre + (count > 0 ? sum / count : 0f), 1f);
        }

        /// <summary>Most cars carry exactly the first car's samples: the backend did not split them by driver.</summary>
        static bool SharePositions(List<ReplayCar> cars)
        {
            if (cars.Count < 2) return false;
            var first = cars[0];
            int same = cars.Skip(1).Count(car => SameSamples(first, car, 50));
            return same * 2 >= cars.Count - 1;
        }

        static bool SameSamples(ReplayCar a, ReplayCar b, int count)
        {
            int n = Mathf.Min(count, Mathf.Min(a.time.Length, b.time.Length));
            if (n == 0) return false;
            for (int i = 0; i < n; i++)
                if (a.time[i] != b.time[i] || a.x[i] != b.x[i] || a.y[i] != b.y[i]) return false;
            return true;
        }

        static ReplayCar BuildCar(int number, CarRows rows, float start)
        {
            float[] time = rows.time.ToArray(), x = rows.x.ToArray(), y = rows.y.ToArray();
            if (!IsSorted(time))
            {
                var order = Enumerable.Range(0, time.Length).OrderBy(i => time[i]).ToArray();
                time = order.Select(i => rows.time[i]).ToArray();
                x = order.Select(i => rows.x[i]).ToArray();
                y = order.Select(i => rows.y[i]).ToArray();
            }

            int lapCount = rows.laps.Select(l => l.lap).DefaultIfEmpty(0).Max();
            var lapTime = Enumerable.Repeat(float.NaN, lapCount + 1).ToArray();
            var sector1 = Enumerable.Repeat(float.NaN, lapCount + 1).ToArray();
            var sector2 = Enumerable.Repeat(float.NaN, lapCount + 1).ToArray();
            foreach (var (lap, duration, s1, s2) in rows.laps)
            {
                lapTime[lap] = duration;
                sector1[lap] = s1;
                sector2[lap] = s2;
            }

            // Lap n+1 starts when lap n ends: lights out plus the lap times. A missing lap
            // time borrows the nearest known one, so one gap does not stop the count.
            var lapStart = Enumerable.Repeat(float.NaN, lapCount + 2).ToArray();
            lapStart[1] = start;
            for (int n = 1; n <= lapCount; n++)
                lapStart[n + 1] = lapStart[n] + Nearest(lapTime, n);

            var positions = rows.positions.OrderBy(p => p.time).ToList();
            var (odometerTime, odometer) = Odometer(rows);
            var car = new ReplayCar
            {
                number = number,
                time = time,
                x = x,
                y = y,
                lapStart = lapStart,
                lapTime = lapTime,
                sector1 = sector1,
                sector2 = sector2,
                lapCount = lapCount,
                positionTime = positions.Select(p => p.time).ToArray(),
                position = positions.Select(p => p.position).ToArray(),
                stints = rows.stints.OrderBy(s => s.lapStart).ToArray(),
                pitLaps = rows.pits.OrderBy(l => l).ToArray(),
                odometerTime = odometerTime,
                odometer = odometer,
            };

            // Typical lap length: the median of the laps after the first (lap 1 starts on the grid).
            var laps = new List<float>();
            for (int n = 2; n <= lapCount; n++)
            {
                float driven = ODReplayMapper.Odometer(car, lapStart[n + 1]) - ODReplayMapper.Odometer(car, lapStart[n]);
                if (driven > 0f) laps.Add(driven);
            }
            laps.Sort();
            if (laps.Count > 0) car.lapDistance = laps[laps.Count / 2];
            return car;
        }

        /// <summary>Metres driven at each speed sample (trapezoids), sorted by time.</summary>
        static (float[] time, float[] metres) Odometer(CarRows rows)
        {
            int n = rows.speedTime.Count;
            var order = Enumerable.Range(0, n).OrderBy(i => rows.speedTime[i]).ToArray();
            var time = new float[n];
            var metres = new float[n];
            for (int k = 0; k < n; k++)
            {
                time[k] = rows.speedTime[order[k]];
                if (k > 0)
                    metres[k] = metres[k - 1] + (rows.speed[order[k]] + rows.speed[order[k - 1]]) * .5f / 3.6f * (time[k] - time[k - 1]);
            }
            return (time, metres);
        }

        static bool IsSorted(float[] values)
        {
            for (int i = 1; i < values.Length; i++) if (values[i] < values[i - 1]) return false;
            return true;
        }

        static float Nearest(float[] lapTime, int lap)
        {
            for (int d = 0; d < lapTime.Length; d++)
            {
                if (lap - d >= 1 && !float.IsNaN(lapTime[lap - d])) return lapTime[lap - d];
                if (lap + d < lapTime.Length && !float.IsNaN(lapTime[lap + d])) return lapTime[lap + d];
            }
            return float.NaN;
        }

        /// <summary>The winner's fastest lap away from the pits (any car with samples when unknown).</summary>
        static Vector2[] Outline(RaceReplay replay)
        {
            var car = replay.cars.Values.Where(c => c.time.Length > 0)
                .OrderBy(c => c.position.Length > 0 ? c.position[c.position.Length - 1] : int.MaxValue)
                .FirstOrDefault();
            if (car == null) return Array.Empty<Vector2>();

            int best = -1;
            for (int n = 2; n <= car.lapCount; n++)
            {
                bool pits = Array.IndexOf(car.pitLaps, n) >= 0 || Array.IndexOf(car.pitLaps, n - 1) >= 0;
                if (!pits && !float.IsNaN(car.lapTime[n]) && (best < 0 || car.lapTime[n] < car.lapTime[best])) best = n;
            }
            if (best < 0) return Array.Empty<Vector2>();

            var points = new List<Vector2>();
            for (int i = 0; i < car.time.Length; i++)
                if (car.time[i] >= car.lapStart[best] && car.time[i] <= car.lapStart[best + 1])
                    points.Add(new Vector2(car.x[i], car.y[i]));
            return points.ToArray();
        }
    }
}
