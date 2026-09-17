using System;

namespace Hangly.Windows.Physics;

/// <summary>
/// A bead as the charm describes it, in proportions relative to the charm's radius.
/// </summary>
public record struct CharmBead(Vector2D Size, double Offset, double Mass)
{
    public CharmBead(double width, double height, double offset, double mass)
        : this(new Vector2D(width, height), offset, mass)
    {
    }

    /// <summary>Extent along the cord, deciding whether two beads collide.</summary>
    public readonly double SpacingRatio => Size.Y / 2.0;
}

/// <summary>
/// A bead being simulated as a constrained Verlet particle riding the cord curve.
/// </summary>
public struct RopeBead : IEquatable<RopeBead>
{
    /// <summary>Current world position.</summary>
    public Vector2D Position;

    /// <summary>Position at the end of the previous step.</summary>
    public Vector2D PreviousPosition;

    /// <summary>Distance along the cord, from the anchor.</summary>
    public double Arc;

    /// <summary>Where the bead rests, measured back from the knot.</summary>
    public double RestOffset;

    /// <summary>Half the bead's extent along the cord, in points.</summary>
    public double SpacingRadius;

    /// <summary>Drawn size in points.</summary>
    public Vector2D Size;

    /// <summary>Mass relative to a plain rope node.</summary>
    public double Mass;

    /// <summary>Orientation of the cord where the bead sits, in radians.</summary>
    public double Angle;

    public RopeBead(
        Vector2D position,
        Vector2D previousPosition,
        double arc,
        double restOffset,
        double spacingRadius,
        Vector2D size,
        double mass,
        double angle)
    {
        Position = position;
        PreviousPosition = previousPosition;
        Arc = arc;
        RestOffset = restOffset;
        SpacingRadius = spacingRadius;
        Size = size;
        Mass = mass;
        Angle = angle;
    }

    /// <summary>Displacement over the last step (implied velocity).</summary>
    public readonly Vector2D Displacement => Position - PreviousPosition;

    /// <summary>How far the bead may travel from its rest place, in points.</summary>
    public readonly double SlideLimit => SpacingRadius * 0.6;

    public readonly bool Equals(RopeBead other) =>
        Position.Equals(other.Position) &&
        PreviousPosition.Equals(other.PreviousPosition) &&
        Arc.Equals(other.Arc) &&
        RestOffset.Equals(other.RestOffset) &&
        SpacingRadius.Equals(other.SpacingRadius) &&
        Size.Equals(other.Size) &&
        Mass.Equals(other.Mass) &&
        Angle.Equals(other.Angle);

    public override readonly bool Equals(object? obj) => obj is RopeBead other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, PreviousPosition, Arc, RestOffset, SpacingRadius, Size, Mass, Angle);
    public static bool operator ==(RopeBead left, RopeBead right) => left.Equals(right);
    public static bool operator !=(RopeBead left, RopeBead right) => !left.Equals(right);
}
