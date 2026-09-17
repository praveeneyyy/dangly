using System;
using System.Windows;

namespace Hangly.Windows.Physics;

/// <summary>
/// Minimal 2D vector arithmetic for the rope solver matching Swift's CGPoint vector extensions.
/// Units are points and seconds throughout.
/// </summary>
public readonly record struct Vector2D(double X, double Y)
{
    public static readonly Vector2D Zero = new(0, 0);

    public static Vector2D operator +(Vector2D lhs, Vector2D rhs) => new(lhs.X + rhs.X, lhs.Y + rhs.Y);
    public static Vector2D operator -(Vector2D lhs, Vector2D rhs) => new(lhs.X - rhs.X, lhs.Y - rhs.Y);
    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);
    public static Vector2D operator *(Vector2D lhs, double scalar) => new(lhs.X * scalar, lhs.Y * scalar);
    public static Vector2D operator *(double scalar, Vector2D rhs) => new(rhs.X * scalar, rhs.Y * scalar);
    public static Vector2D operator /(Vector2D lhs, double scalar) => new(lhs.X / scalar, lhs.Y / scalar);

    public static implicit operator Point(Vector2D v) => new(v.X, v.Y);
    public static implicit operator Vector2D(Point p) => new(p.X, p.Y);

    /// <summary>
    /// Euclidean length.
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Length without the square root, for fast distance comparisons.
    /// </summary>
    public double MagnitudeSquared => X * X + Y * Y;

    /// <summary>
    /// Unit vector, or zero for a zero-length vector.
    /// </summary>
    public Vector2D Normalized
    {
        get
        {
            double length = Magnitude;
            return length > double.Epsilon ? this / length : Zero;
        }
    }

    public double DistanceTo(Vector2D other) => (other - this).Magnitude;

    /// <summary>
    /// Returns the vector rotated counter-clockwise by radians.
    /// </summary>
    public Vector2D RotatedBy(double radians)
    {
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);
        return new Vector2D(X * cosine - Y * sine, X * sine + Y * cosine);
    }

    /// <summary>
    /// Caps the vector's length at maximum, preserving direction.
    /// </summary>
    public Vector2D LimitedTo(double maximum)
    {
        double length = Magnitude;
        if (length > maximum && length > double.Epsilon)
        {
            return this * (maximum / length);
        }
        return this;
    }

    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
}
