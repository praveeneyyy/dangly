using System.Collections.Generic;

namespace Hangly.Windows.Physics;

/// <summary>
/// Geometry for rendering a single bead along the rope.
/// </summary>
public readonly record struct BeadPlacement(Vector2D Position, double Angle, Vector2D Size);

/// <summary>
/// Read-only snapshot of the rope and charm geometry produced by the physics solver for the renderer.
/// </summary>
public sealed record RopeSnapshot(
    IReadOnlyList<Vector2D> Points,
    double CharmRadius,
    double CharmAngle,
    double CharmKnotInset,
    IReadOnlyList<BeadPlacement> Beads,
    double MaximumStretch,
    bool IsDragging
);
