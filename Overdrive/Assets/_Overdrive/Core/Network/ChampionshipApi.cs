/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipApi - Championship endpoints proxied by the gateway under
 ## /v1/championship/* (see gateway/ROUTES.md). Every method fetches DTOs
 ## then applies them onto the caller-owned target (entity or List<Entity>)
 ## in place - never returns a new instance (see Data/Doc/UI-Refresh-Pattern.md).
 ##
 */

using System;
using System.Collections;
using System.Collections.Generic;

public static class ChampionshipApi
{
    private const string ChampionshipPrefix = "/v1/championship";

    // Catalog - static, fetched once (see Logic/Doc/UI-Refresh-Pattern.md)

    // Every championship available.
    public static IEnumerator GetChampionships(List<Championship> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<ChampionshipDTO>>($"{ChampionshipPrefix}/championships", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.championshipCode, entity => entity.code,
                () => new Championship(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Calendar of one championship - each entry still missing championshipCode/circuit until GetEventDetail fills them in.
    public static IEnumerator GetEvents(string championshipCode, List<ChampionshipEvent> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<EventSummaryDTO>>($"{ChampionshipPrefix}/championships/{championshipCode}/events", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.eventId, entity => entity.id,
                () => new ChampionshipEvent(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Single-entity fetch, not a list - no EntityCollectionSync involved, just TryApply straight onto target.
    public static IEnumerator GetEventDetail(string eventId, ChampionshipEvent target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<EventDetailDTO>($"{ChampionshipPrefix}/events/{eventId}", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }

    // Sessions of one event (practice/qualifying/race/sprint).
    public static IEnumerator GetSessions(string eventId, List<RaceSession> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<SessionSummaryDTO>>($"{ChampionshipPrefix}/events/{eventId}/sessions", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.sessionId, entity => entity.id,
                () => new RaceSession(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Single-entity fetch - fills in eventId/circuit/weather that GetSessions doesn't carry.
    public static IEnumerator GetSessionDetail(string sessionId, RaceSession target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<SessionDetailDTO>($"{ChampionshipPrefix}/sessions/{sessionId}", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }

    // Drivers entered in one session - has teamName but not nationality/picture (see GetDriverProfile).
    public static IEnumerator GetSessionDrivers(string sessionId, List<Driver> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<SessionDriverDTO>>($"{ChampionshipPrefix}/sessions/{sessionId}/drivers", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.driverNumber, entity => entity.number,
                () => new Driver(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Teams entered in one session.
    public static IEnumerator GetSessionTeams(string sessionId, List<Team> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<TeamDTO>>($"{ChampionshipPrefix}/sessions/{sessionId}/teams", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.teamId, entity => entity.id,
                () => new Team(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Live - meant to be called on a repeating timer while the session is live.
    public static IEnumerator GetStandings(string sessionId, List<StandingEntry> target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<List<StandingEntryDTO>>($"{ChampionshipPrefix}/sessions/{sessionId}/standings/race", dtos =>
        {
            EntityCollectionSync.Sync(target, dtos,
                dto => dto.driverNumber, entity => entity.driverNumber,
                () => new StandingEntry(), (dto, entity) => dto.TryApply(entity));
            onSuccess?.Invoke();
        }, onError);
    }

    // Driver profile - has nationality/picture that GetSessionDrivers doesn't carry, needs a sessionId too.
    public static IEnumerator GetDriverProfile(string sessionId, int driverNumber, Driver target, Action onSuccess, Action<DataError> onError)
    {
        yield return ApiClient.Get<DriverProfileDTO>($"{ChampionshipPrefix}/sessions/{sessionId}/drivers/{driverNumber}/profile", dto =>
        {
            DataError? error = dto.TryApply(target);
            if (error != null) { onError?.Invoke(error.Value); return; }
            onSuccess?.Invoke();
        }, onError);
    }
}
