/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## WidgetCatalog - What the widget catalogue (opened from the "Telemetry"
 ## button of the race page) lists: the categories of the side bar and the
 ## widgets of each one, with whether they are already in the user's view.
 ## Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;

public enum WidgetFormat { Small, Large }

[Serializable]
public class WidgetCategory
{
    public string id;            // "car"
    public string name;          // "Car data"
    public string description;   // "The car of the followed driver"
}

[Serializable]
public class WidgetDefinition
{
    public string id;            // "speed"
    public string categoryId;    // "car"
    public string name;          // "Speed"
    public string description;   // "Live speed of the selected driver"
    public List<WidgetFormat> formats = new() { WidgetFormat.Small, WidgetFormat.Large };
    public bool added;           // already in the user's view
}

[Serializable]
public class WidgetCatalog
{
    public List<WidgetCategory> categories = new();
    public List<WidgetDefinition> widgets = new();

    public List<WidgetDefinition> In(string categoryId) => widgets.FindAll(w => w.categoryId == categoryId);

    public int CountIn(string categoryId) => widgets.FindAll(w => w.categoryId == categoryId).Count;

    public int AddedCount => widgets.FindAll(w => w.added).Count;
}
