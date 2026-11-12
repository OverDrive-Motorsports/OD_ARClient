/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayPlayer - Plays a RaceReplay. Load() puts the cars on the grid at
 ## lights out with the clock stopped; Begin() starts the clock, which then
 ## runs in real time until Stop() or the end of the data (no controls yet).
 ## Moves the cars on the track map every frame, and tells the page when the
 ## running order or the lap/clock line changes.
 ##
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ODReplayPlayer : MonoBehaviour
{
    [Tooltip("Race seconds per real second.")]
    public float speed = 1f;
    [Tooltip("Seconds between two computations of the running order.")]
    public float orderRefreshSeconds = .5f;

    /// <summary>Running order, P1 first — raised only when something shown in it changes.</summary>
    public event Action<LiveSessionData> OrderChanged;
    /// <summary>"Tour 12 / 56 · 0:21:45" — raised once per race second.</summary>
    public event Action<string> ProgressChanged;

    /// <summary>A replay is loaded (on the grid, or playing).</summary>
    public bool IsLoaded => _replay != null;
    /// <summary>The clock runs: Begin() was called.</summary>
    public bool IsPlaying => _replay != null && _running;
    /// <summary>Current time, in RaceReplay seconds.</summary>
    public float RaceTime { get; private set; }

    RaceReplay _replay;
    ReplaySummary _summary;
    ODReplayTrackMap _map;
    bool _running;
    readonly Dictionary<int, int> _sampleHints = new Dictionary<int, int>();
    LiveSessionData _frame;
    string _orderKey;
    float _nextOrder;
    int _second = -1;

    /// <summary>Cars on the grid at lights out, clock stopped. <paramref name="map"/> may be null (running order only).</summary>
    public void Load(RaceReplay replay, ReplaySummary summary, ODReplayTrackMap map)
    {
        Stop();
        if (replay == null) return;
        _replay = replay;
        _summary = summary;
        _map = map;
        RaceTime = replay.start;
        if (_map != null)
        {
            _map.Show(replay);
            _map.SetPlaceholder(true);   // the play disc stays over the grid until the start
        }
        MoveCars();
    }

    /// <summary>Starts the clock from lights out (no effect when already playing).</summary>
    public void Begin()
    {
        if (_replay == null || _running) return;
        _running = true;
        _map?.SetPlaceholder(false);
        Tick(forceOrder: true);
    }

    public void Stop()
    {
        if (_replay == null) return;
        _map?.Hide();
        _replay = null;
        _map = null;
        _running = false;
        _frame = null;
        _orderKey = null;
        _second = -1;
        _sampleHints.Clear();
    }

    void Update()
    {
        if (_replay == null || !_running) return;
        RaceTime = Mathf.Min(RaceTime + ODTime.Delta * speed, _replay.end);
        Tick(forceOrder: false);
    }

    void OnDisable() => Stop();

    void MoveCars()
    {
        if (_map == null) return;
        foreach (var car in _replay.cars.Values)
        {
            if (!_sampleHints.TryGetValue(car.number, out int hint)) hint = -1;
            Vector2 position = default;
            bool visible = !ODReplayMapper.IsRetired(_replay, car, RaceTime) &&
                           ODReplayMapper.TryGetPosition(_replay, car, RaceTime, ref hint, out position);
            _sampleHints[car.number] = hint;
            _map.MoveCar(car.number, position, visible);
        }
    }

    void Tick(bool forceOrder)
    {
        MoveCars();

        bool orderChanged = false;
        if (forceOrder || ODTime.Now >= _nextOrder)
        {
            _nextOrder = ODTime.Now + orderRefreshSeconds;
            _frame = ODReplayMapper.ToFrame(_replay, _summary, RaceTime);
            string key = string.Join("|", _frame.entries.Select(e => $"{e.name};{e.gap};{e.tyreCompound}{e.tyreAgeLaps};{e.pitStops};{e.lastLap}"));
            if (key != _orderKey)
            {
                _orderKey = key;
                orderChanged = true;
                OrderChanged?.Invoke(_frame);
                if (_replay == null) return;   // the page stopped playback from the event
            }
        }

        int second = Mathf.FloorToInt(RaceTime - _replay.start);
        if (second != _second || orderChanged)
        {
            _second = second;
            ProgressChanged?.Invoke(ODReplayMapper.Progress(_frame, RaceTime - _replay.start));
        }
    }
}
