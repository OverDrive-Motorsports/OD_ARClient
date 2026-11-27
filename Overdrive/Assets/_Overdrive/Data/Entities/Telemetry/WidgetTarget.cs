/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## WidgetTarget - Which car a telemetry widget placed in the space follows
 ## (design "Pilote associé"):
 ##   • Position — whoever runs at that place, changing on its own when the
 ##     order changes (P1, the leader, is the default; P2 for the second car
 ##     of a widget comparing two);
 ##   • Driver — one car, by number, whatever its position.
 ## DriverChoice is one car of the running order as the driver picker lists
 ## it ("P1 ▌VER Max Verstappen").
 ## Pure data: no MonoBehaviour, no logic.
 ##
 */

using System;
using UnityEngine;

public enum WidgetTargetKind { Position, Driver }

public readonly struct WidgetTarget : IEquatable<WidgetTarget>
{
    public readonly WidgetTargetKind kind;
    /// <summary>Running position (Position, 1 = leader) or car number (Driver).</summary>
    public readonly int value;

    WidgetTarget(WidgetTargetKind kind, int value)
    {
        this.kind = kind;
        this.value = value;
    }

    /// <summary>The car running at <paramref name="position"/> (1 = leader).</summary>
    public static WidgetTarget Position(int position) => new(WidgetTargetKind.Position, Math.Max(1, position));
    public static WidgetTarget Leader => Position(1);
    public static WidgetTarget Driver(int number) => new(WidgetTargetKind.Driver, number);

    public bool Equals(WidgetTarget other) => kind == other.kind && value == other.value;
    public override bool Equals(object obj) => obj is WidgetTarget other && Equals(other);
    public override int GetHashCode() => (int)kind * 397 ^ value;
    public static bool operator ==(WidgetTarget a, WidgetTarget b) => a.Equals(b);
    public static bool operator !=(WidgetTarget a, WidgetTarget b) => !a.Equals(b);
}

/// <summary>One car of the running order, as the driver picker lists it.</summary>
public sealed class DriverChoice
{
    /// <summary>Running position, 1 = leader (0 when unknown).</summary>
    public int position;
    public int number;
    public string code = "";
    public string fullName = "";
    public Color teamColor = Color.white;
}
