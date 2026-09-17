using System;

namespace Hangly.Windows.Physics;

/// <summary>
/// The physical properties a charm contributes to the rope.
/// </summary>
public record struct CharmMetrics(double Mass, double RadiusRatio, double KnotInset = 0.90)
{
    /// <summary>
    /// The shipped default, matching the plain bead.
    /// </summary>
    public static readonly CharmMetrics Default = new(Mass: 2.6, RadiusRatio: 0.126, KnotInset: 0.90);

    /// <summary>
    /// Linear blend, used to make a charm change resize smoothly instead of popping.
    /// </summary>
    public static CharmMetrics Interpolate(CharmMetrics start, CharmMetrics end, double progress)
    {
        double clamped = Math.Clamp(progress, 0.0, 1.0);
        return new CharmMetrics(
            Mass: start.Mass + ((end.Mass - start.Mass) * clamped),
            RadiusRatio: start.RadiusRatio + ((end.RadiusRatio - start.RadiusRatio) * clamped),
            KnotInset: start.KnotInset + ((end.KnotInset - start.KnotInset) * clamped)
        );
    }
}
