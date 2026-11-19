/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## WidgetCatalog - What the widget catalogue (opened from the "Telemetry"
 ## button of the race page) lists: the categories of the side bar and the
 ## widgets of each one, with how many of each are in the user's space.
 ## The "suggestions" category lists the widgets flagged as suggested, from
 ## every category. Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using System.Collections.Generic;

/// <summary>The two sizes every telemetry widget comes in (Small 440 x 440, Large 920 x 440 design px).</summary>
public enum WidgetFormat { Small, Large }

[Serializable]
public class WidgetCategory
{
    public const string SuggestionsId = "suggestions";

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
    public bool suggested;       // also listed under "Suggestions"
    public int inSpace;          // how many of it are in the user's space

    public bool Added => inSpace > 0;
}

[Serializable]
public class WidgetCatalog
{
    public List<WidgetCategory> categories = new();
    public List<WidgetDefinition> widgets = new();

    public List<WidgetDefinition> In(string categoryId) => widgets.FindAll(w => Belongs(w, categoryId));

    public int CountIn(string categoryId) => widgets.FindAll(w => Belongs(w, categoryId)).Count;

    public WidgetDefinition Find(string id) => widgets.Find(w => w.id == id);

    /// <summary>Widgets in the user's space, all kinds together.</summary>
    public int AddedCount
    {
        get
        {
            int count = 0;
            foreach (var widget in widgets) count += widget.inSpace;
            return count;
        }
    }

    static bool Belongs(WidgetDefinition widget, string categoryId) =>
        widget.categoryId == categoryId || (categoryId == WidgetCategory.SuggestionsId && widget.suggested);
}
