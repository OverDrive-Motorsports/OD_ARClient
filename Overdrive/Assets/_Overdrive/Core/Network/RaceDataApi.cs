/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## RaceDataApi - Race/telemetry endpoints proxied by the gateway under
 ## /v1/race-data/* (see the backend's endpoints doc). Same simple shape as
 ## ChampionshipApi.cs: coroutine + ApiClient.Get/Post, fetch DTOs then
 ## apply them onto the caller-owned target in place.
 ##
 ## Left out on purpose (no DTO/confirmed shape yet): result, starting_grid,
 ## session_result, overtakes, car_data, /race/live/stream (SSE - see
 ## Core/Doc/README.md, different transport entirely, not wired here).
 ##
 */

using System;
using System.Collections;
using System.Collections.Generic;

public static class RaceDataApi
{
    private const string Prefix = "/v1/race-data";

    // Session-wide - last known position of every driver.
    public static IEnumerator GetPositions(string sessionId, List<RacePosition> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<RacePositionDTO>>($"{Prefix}/sessions/{sessionId}/race/position", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.driverNumber, entity => entity.driverNumber,
                () => new RacePosition(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Single-entity fetch - the same path returns one object instead of an array when driverNumber is passed.
    public static IEnumerator GetDriverPosition(string sessionId, int driverNumber, RacePosition target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<RacePositionDTO>($"{Prefix}/sessions/{sessionId}/race/position?driverNumber={driverNumber}", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }

    // Session-wide - lap times/sectors for every driver (one DriverLaps entry each).
    public static IEnumerator GetLaps(string sessionId, List<DriverLaps> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<RaceLapsDTO>>($"{Prefix}/sessions/{sessionId}/race/laps", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.driverNumber, entity => entity.driverNumber,
                () => new DriverLaps(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Single-entity fetch - the same path returns one object instead of an array when driverNumber is passed.
    public static IEnumerator GetDriverLaps(string sessionId, int driverNumber, DriverLaps target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<RaceLapsDTO>($"{Prefix}/sessions/{sessionId}/race/laps?driverNumber={driverNumber}", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }

    // Session-wide - always an array, driverNumber (if ever added) would just filter it.
    public static IEnumerator GetStints(string sessionId, List<Stint> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<StintDTO>>($"{Prefix}/sessions/{sessionId}/race/stints", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => (dto.driverNumber, dto.stintNumber), entity => (entity.driverNumber, entity.stintNumber),
                () => new Stint(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Session-wide - always an array.
    public static IEnumerator GetPitStops(string sessionId, List<PitStop> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<PitStopDTO>>($"{Prefix}/sessions/{sessionId}/race/pitstops", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => (dto.driverNumber, dto.lapNumber), entity => (entity.driverNumber, entity.lapNumber),
                () => new PitStop(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Session-wide - always an array, no filter.
    public static IEnumerator GetWeather(string sessionId, List<WeatherSample> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<RaceWeatherSampleDTO>>($"{Prefix}/sessions/{sessionId}/race/weather", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.timestamp, entity => entity.timestamp,
                () => new WeatherSample(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Session-wide - always an array.
    public static IEnumerator GetTeamRadio(string sessionId, List<TeamRadioMessage> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<TeamRadioMessageDTO>>($"{Prefix}/sessions/{sessionId}/race/radio", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.audioUrl, entity => entity.audioUrl,
                () => new TeamRadioMessage(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // POST, not GET - the backend holds the request open up to 30s waiting for a new event and
    // returns [] on timeout (not an error). Call this again immediately after it returns to keep
    // listening - it's the caller's job to loop, this method only does a single long-poll call.
    public static IEnumerator GetRaceControlEvents(string sessionId, List<RaceControlEvent> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Post<List<RaceControlEventDTO>>($"{Prefix}/sessions/{sessionId}/race/control", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.timestamp, entity => entity.timestamp,
                () => new RaceControlEvent(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Driver-scoped - full speed sample history.
    public static IEnumerator GetDriverSpeedHistory(string sessionId, int driverNumber, List<SpeedSample> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<SpeedSampleDTO>>($"{Prefix}/sessions/{sessionId}/drivers/{driverNumber}/telemetry/speed", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.timestamp, entity => entity.timestamp,
                () => new SpeedSample(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Driver-scoped - full engine sample history.
    public static IEnumerator GetDriverEngineHistory(string sessionId, int driverNumber, List<EngineSample> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<EngineSampleDTO>>($"{Prefix}/sessions/{sessionId}/drivers/{driverNumber}/telemetry/engine", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.timestamp, entity => entity.timestamp,
                () => new EngineSample(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Driver-scoped - full location sample history.
    public static IEnumerator GetDriverLocationHistory(string sessionId, int driverNumber, List<LocationSample> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<LocationSampleDTO>>($"{Prefix}/sessions/{sessionId}/drivers/{driverNumber}/telemetry/location", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.timestamp, entity => entity.timestamp,
                () => new LocationSample(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Driver-scoped - single-entity fetch, latest known sample only, no lapNumber filter.
    public static IEnumerator GetDriverIntervals(string sessionId, int driverNumber, DriverIntervals target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<IntervalsDTO>($"{Prefix}/sessions/{sessionId}/drivers/{driverNumber}/telemetry/intervals", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }
}
