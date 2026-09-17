using System;
using System.Collections.Generic;

namespace Hangly.Windows.Physics;

/// <summary>
/// One node of the rope represented as a Verlet particle.
/// Verlet integration stores no explicit velocity: velocity is implied by the displacement
/// between where it is and where it was.
/// </summary>
public struct RopePoint : IEquatable<RopePoint>
{
    /// <summary>Current position in canvas coordinates.</summary>
    public Vector2D Position;

    /// <summary>Position at the end of the previous step.</summary>
    public Vector2D PreviousPosition;

    /// <summary>
    /// Reciprocal of mass. Zero pins the node in place: constraint corrections
    /// scaled by zero move it not at all.
    /// </summary>
    public double InverseMass;

    public RopePoint(Vector2D position, double inverseMass = 1.0)
    {
        Position = position;
        PreviousPosition = position;
        InverseMass = inverseMass;
    }

    /// <summary>Displacement over the last step.</summary>
    public readonly Vector2D Displacement => Position - PreviousPosition;

    public readonly bool IsPinned => InverseMass == 0.0;

    /// <summary>Sets the implied velocity, in points per second, for a given step length.</summary>
    public void SetVelocity(Vector2D velocity, double timeStep)
    {
        PreviousPosition = Position - (velocity * timeStep);
    }

    public static List<RopePoint> CreateChain(
        RopeConfiguration configuration,
        Vector2D anchor,
        CharmMetrics charmMetrics,
        double angle)
    {
        var direction = new Vector2D(0, 1).RotatedBy(angle);
        int lastIndex = configuration.PointCount - 1;
        var points = new List<RopePoint>(configuration.PointCount);

        for (int index = 0; index <= lastIndex; index++)
        {
            double inverseMass;
            if (index == 0)
            {
                inverseMass = 0.0;
            }
            else if (index == lastIndex)
            {
                inverseMass = 1.0 / Math.Max(charmMetrics.Mass, 0.0001);
            }
            else
            {
                inverseMass = 1.0;
            }

            var offset = direction * (index * configuration.SegmentLength);
            points.Add(new RopePoint(anchor + offset, inverseMass));
        }

        return points;
    }

    public readonly bool Equals(RopePoint other) =>
        Position.Equals(other.Position) &&
        PreviousPosition.Equals(other.PreviousPosition) &&
        InverseMass.Equals(other.InverseMass);

    public override readonly bool Equals(object? obj) => obj is RopePoint other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, PreviousPosition, InverseMass);
    public static bool operator ==(RopePoint left, RopePoint right) => left.Equals(right);
    public static bool operator !=(RopePoint left, RopePoint right) => !left.Equals(right);
}
