/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODReplayTrackMap - Organism drawn over the replay card's screen: the
 ## circuit outline and one dot per car in its team colour. Display only:
 ## ODReplayPlayer says where each car is.
 ##
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using D = ODDesign;

public sealed class ODReplayTrackMap : MonoBehaviour
{
    // Design px.
    const float Padding = 30f, Line = 7f, Dot = 13f, Ring = 2f;

    RectTransform _rect;
    ODTrackLine _line;
    readonly Dictionary<int, RectTransform> _cars = new Dictionary<int, RectTransform>();
    Vector2 _center;   // track units
    float _scale;      // local units per track unit

    /// <summary>The map of <paramref name="screen"/>, created on first use (filling it).</summary>
    public static ODReplayTrackMap For(RectTransform screen)
    {
        if (screen == null) return null;
        var map = screen.GetComponentInChildren<ODReplayTrackMap>(true);
        if (map != null) return map;

        map = Stretch(new GameObject("ReplayTrackMap", typeof(RectTransform)), screen).gameObject.AddComponent<ODReplayTrackMap>();
        map._rect = (RectTransform)map.transform;
        map._line = Stretch(new GameObject("Track", typeof(RectTransform)), map._rect).gameObject.AddComponent<ODTrackLine>();
        map._line.color = D.WithAlpha(Color.white, .34f);
        map._line.thickness = D.Px(Line);
        map._line.raycastTarget = false;
        map.gameObject.SetActive(false);
        return map;
    }

    /// <summary>Fits the circuit to the screen and creates the cars, hidden until their first position.</summary>
    public void Show(RaceReplay replay)
    {
        gameObject.SetActive(true);
        SetPlaceholder(false);

        var outline = replay.outline;
        if (outline.Length == 0)   // no clean lap: frame every car sample instead
            outline = replay.cars.Values.SelectMany(c => c.x.Zip(c.y, (x, y) => new Vector2(x, y))).ToArray();
        Fit(outline);
        _line.SetPoints(replay.outline.Select(ToLocal).ToArray());

        foreach (var car in _cars.Values) Destroy(car.gameObject);
        _cars.Clear();
        foreach (var car in replay.cars.Values.OrderByDescending(c => c.number))
        {
            replay.drivers.TryGetValue(car.number, out var driver);
            _cars[car.number] = CreateCar(car.number, ODChampionshipMapper.ParseColor(driver?.teamColor));
        }
    }

    public void MoveCar(int number, Vector2 trackPosition, bool visible)
    {
        if (!_cars.TryGetValue(number, out var car)) return;
        if (car.gameObject.activeSelf != visible) car.gameObject.SetActive(visible);
        if (visible) car.anchoredPosition = ToLocal(trackPosition);
    }

    public void Hide()
    {
        foreach (var car in _cars.Values) Destroy(car.gameObject);
        _cars.Clear();
        _line.SetPoints(null);
        gameObject.SetActive(false);
        SetPlaceholder(true);
    }

    void Fit(Vector2[] points)
    {
        if (points.Length == 0) { _center = Vector2.zero; _scale = 0f; return; }
        Vector2 min = points[0], max = points[0];
        foreach (var p in points) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        _center = (min + max) * .5f;
        Vector2 room = _rect.rect.size - Vector2.one * D.Px(Padding) * 2f;
        Vector2 size = Vector2.Max(max - min, Vector2.one);
        _scale = Mathf.Max(0f, Mathf.Min(room.x / size.x, room.y / size.y));
    }

    /// <summary>Track x/y (y up) to local units from the map's centre.</summary>
    Vector2 ToLocal(Vector2 trackPosition) => (trackPosition - _center) * _scale;

    RectTransform CreateCar(int number, Color team)
    {
        var ring = D.Fill(_rect, "Car_" + number, 0f, 0f, Dot, Dot, Color.white, Dot * .5f);
        ring.raycastTarget = false;
        var fill = D.Fill(ring.transform, "Team", Ring, Ring, Dot - Ring * 2f, Dot - Ring * 2f, team, (Dot - Ring * 2f) * .5f);
        fill.raycastTarget = false;

        var rect = ring.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.gameObject.SetActive(false);
        return rect;
    }

    /// <summary>The screen's play disc: shown over the map until the race starts, hidden while it runs.</summary>
    public void SetPlaceholder(bool visible)
    {
        var play = transform.parent != null ? transform.parent.Find("Play") : null;
        if (play == null) return;
        play.gameObject.SetActive(visible);
        if (visible) play.SetAsLastSibling();
    }

    static RectTransform Stretch(GameObject go, Transform parent)
    {
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.layer = parent.gameObject.layer;
        return rect;
    }
}
