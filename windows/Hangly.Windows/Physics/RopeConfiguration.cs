using System;
using System.Windows;

namespace Hangly.Windows.Physics;

/// <summary>
/// Every number the rope solver depends on, in one value type.
/// Units are points and seconds throughout.
/// The canvas origin is top-left with Y increasing downward, so gravity is a positive Y acceleration.
/// </summary>
public record struct RopeConfiguration
{
    /// <summary>Number of links. Twenty is the shipped value.</summary>
    public int SegmentCount { get; set; } = 20;

    /// <summary>Rest length of one link, in points.</summary>
    public double SegmentLength { get; set; } = 11.0;

    /// <summary>Downward acceleration in points per second squared.</summary>
    public double Gravity { get; set; } = 2000.0;

    /// <summary>Fraction of velocity carried into the next step. Below 1 this is air drag.</summary>
    public double Damping { get; set; } = 0.999;

    /// <summary>Gauss-Seidel relaxation passes per step.</summary>
    public int ConstraintIterations { get; set; } = 256;

    /// <summary>Inequality-projection sweeps applied after relaxation.</summary>
    public int StretchPasses { get; set; } = 256;

    /// <summary>Relaxation stops early once no link moved further than this in a whole pass.</summary>
    public double ConvergenceTolerance { get; set; } = 0.05;

    /// <summary>Hard ceiling on how far a link may exceed its rest length, as a ratio.</summary>
    public double MaxStretchRatio { get; set; } = 1.02;

    /// <summary>Physics advances in fixed slices of this length regardless of display refresh rate.</summary>
    public double FixedTimeStep { get; set; } = 1.0 / 240.0;

    /// <summary>Upper bound on the real time consumed by one frame (clamping accumulator).</summary>
    public double MaxFrameDuration { get; set; } = 0.1;

    /// <summary>Speed ceiling in points per second, applied per node.</summary>
    public double MaximumSpeed { get; set; } = 6000.0;

    /// <summary>How far the charm may be dragged from the anchor, as a fraction of total length.</summary>
    public double MaximumReachRatio { get; set; } = 0.98;

    /// <summary>Node speed, in points per second, below which the rope counts as still.</summary>
    public double RestSpeed { get; set; } = 4.0;

    /// <summary>Consecutive still frames before the solver stops working.</summary>
    public int FramesBeforeSleep { get; set; } = 60;

    /// <summary>Angle from vertical the rope is released at on first appearance, in radians.</summary>
    public double InitialAngle { get; set; } = 0.38;

    /// <summary>Number of nodes, which is one more than the number of links.</summary>
    public readonly int PointCount => SegmentCount + 1;

    /// <summary>Total rest length of the rope.</summary>
    public readonly double TotalLength => SegmentCount * SegmentLength;

    public RopeConfiguration() { }

    public static readonly RopeConfiguration Default = new();

    /// <summary>
    /// Fits the rope to a canvas, keeping the shipped proportions at any scale.
    /// </summary>
    public static RopeConfiguration Fitted(Size size)
    {
        var configuration = Default;
        double usableLength = Math.Max(40.0, size.Height * Layout.LengthFraction);
        configuration.SegmentLength = usableLength / configuration.SegmentCount;
        return configuration;
    }

    /// <summary>
    /// Proportions shared by the solver and the renderer.
    /// </summary>
    public static class Layout
    {
        /// <summary>Rope length as a fraction of canvas height.</summary>
        public const double LengthFraction = 0.69;

        /// <summary>Anchor height as a fraction of canvas height.</summary>
        public const double AnchorFraction = 0.045;

        /// <summary>Extra radius around the charm that still accepts a grab.</summary>
        public const double GrabPadding = 10.0;

        /// <summary>Anchor point for a canvas of the given size.</summary>
        public static Vector2D Anchor(Size size)
        {
            return new Vector2D(size.Width / 2.0, size.Height * AnchorFraction);
        }
    }
}
