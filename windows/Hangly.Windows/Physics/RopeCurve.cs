using System;
using System.Collections.Generic;

namespace Hangly.Windows.Physics;

/// <summary>
/// The cord as a curve that can be walked by arc length.
/// Flattened quadratic spline through node midpoints (4 samples per segment).
/// </summary>
public sealed class RopeCurve
{
    private readonly List<Vector2D> _samples = [];
    private readonly List<double> _cumulative = [];

    public IReadOnlyList<Vector2D> Samples => _samples;
    public IReadOnlyList<double> Cumulative => _cumulative;

    /// <summary>Total length of the drawn cord, in points.</summary>
    public double Length => _cumulative.Count > 0 ? _cumulative[^1] : 0.0;

    public bool IsEmpty => _samples.Count < 2;

    public RopeCurve() { }

    public RopeCurve(IReadOnlyList<Vector2D> points, Vector2D end, int samplesPerSegment = 4)
    {
        Rebuild(points, end, samplesPerSegment);
    }

    /// <summary>
    /// Re-measures the curve in place, reusing storage.
    /// </summary>
    public void Rebuild(IReadOnlyList<Vector2D> points, Vector2D end, int samplesPerSegment = 4)
    {
        _samples.Clear();
        _cumulative.Clear();

        if (points.Count < 2) return;

        var first = points[0];
        _samples.Add(first);

        if (points.Count > 2)
        {
            var start = first;
            for (int index = 1; index < points.Count - 1; index++)
            {
                var control = points[index];
                var finish = (points[index] + points[index + 1]) * 0.5;
                for (int step = 1; step <= samplesPerSegment; step++)
                {
                    double fraction = (double)step / samplesPerSegment;
                    _samples.Add(Quadratic(start, control, finish, fraction));
                }
                start = finish;
            }
        }

        _samples.Add(end);

        _cumulative.Add(0.0);
        double total = 0.0;
        for (int index = 1; index < _samples.Count; index++)
        {
            total += _samples[index].DistanceTo(_samples[index - 1]);
            _cumulative.Add(total);
        }
    }

    private static Vector2D Quadratic(Vector2D start, Vector2D control, Vector2D end, double fraction)
    {
        double inverse = 1.0 - fraction;
        var toStart = start * (inverse * inverse);
        var toControl = control * (2.0 * inverse * fraction);
        var toEnd = end * (fraction * fraction);
        return toStart + toControl + toEnd;
    }

    /// <summary>
    /// The flattened curve up to arc, as a polyline that can be stroked.
    /// </summary>
    public List<Vector2D> Polyline(double upToArc)
    {
        if (IsEmpty) return [];
        double cut = Math.Clamp(upToArc, 0.0, Length);
        var result = new List<Vector2D>(_samples.Count);
        for (int index = 0; index < _samples.Count; index++)
        {
            if (_cumulative[index] >= cut) break;
            result.Add(_samples[index]);
        }
        result.Add(PointAtArc(cut));
        return result;
    }

    /// <summary>
    /// Where the curve last crosses into a circle of radius around center.
    /// </summary>
    public double ArcEnteringCircleAround(Vector2D center, double radius)
    {
        if (IsEmpty) return 0.0;
        if (radius <= 0.0) return Length;

        int index = _samples.Count - 1;
        while (index > 0)
        {
            double outer = _samples[index - 1].DistanceTo(center);
            if (outer < radius)
            {
                index--;
                continue;
            }
            double inner = _samples[index].DistanceTo(center);
            double span = outer - inner;
            double fraction = span > double.Epsilon ? Math.Clamp((outer - radius) / span, 0.0, 1.0) : 0.0;
            return _cumulative[index - 1] + ((_cumulative[index] - _cumulative[index - 1]) * fraction);
        }
        return 0.0;
    }

    /// <summary>
    /// The point this far along the cord, clamped to its ends.
    /// </summary>
    public Vector2D PointAtArc(double arc)
    {
        if (IsEmpty) return Vector2D.Zero;
        double target = Math.Clamp(arc, 0.0, Length);
        int index = SegmentIndex(target);
        double spanStart = _cumulative[index];
        double spanLength = _cumulative[index + 1] - spanStart;
        if (spanLength <= double.Epsilon) return _samples[index];
        double fraction = (target - spanStart) / spanLength;
        return _samples[index] + ((_samples[index + 1] - _samples[index]) * fraction);
    }

    /// <summary>
    /// Direction of travel along the cord at this distance, in radians.
    /// </summary>
    public double AngleAtArc(double arc)
    {
        if (IsEmpty) return Math.PI / 2.0;
        int index = SegmentIndex(Math.Clamp(arc, 0.0, Length));
        var delta = _samples[index + 1] - _samples[index];
        if (delta.MagnitudeSquared <= double.Epsilon) return Math.PI / 2.0;
        return Math.Atan2(delta.Y, delta.X);
    }

    /// <summary>
    /// Distance along the cord of the point closest to location within a search window.
    /// </summary>
    public double ArcNearestTo(Vector2D location, double near, double window)
    {
        if (IsEmpty) return 0.0;
        double lower = Math.Clamp(near - window, 0.0, Length);
        double upper = Math.Clamp(near + window, 0.0, Length);

        double best = near;
        double bestDistance = double.PositiveInfinity;
        for (int index = 0; index < _samples.Count - 1; index++)
        {
            if (_cumulative[index + 1] < lower || _cumulative[index] > upper) continue;
            var (arc, distance) = ClosestPointOnSegment(index, location);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = arc;
            }
        }
        return Math.Clamp(best, lower, upper);
    }

    private (double Arc, double Distance) ClosestPointOnSegment(int index, Vector2D location)
    {
        var start = _samples[index];
        var end = _samples[index + 1];
        var span = end - start;
        double lengthSquared = span.MagnitudeSquared;
        if (lengthSquared <= double.Epsilon)
        {
            return (_cumulative[index], start.DistanceTo(location));
        }
        var offset = location - start;
        double fraction = Math.Clamp(((offset.X * span.X) + (offset.Y * span.Y)) / lengthSquared, 0.0, 1.0);
        var projected = start + (span * fraction);
        double arc = _cumulative[index] + ((_cumulative[index + 1] - _cumulative[index]) * fraction);
        return (arc, projected.DistanceTo(location));
    }

    private int SegmentIndex(double arc)
    {
        int low = 0;
        int high = _cumulative.Count - 1;
        while (low < high - 1)
        {
            int middle = (low + high) / 2;
            if (_cumulative[middle] <= arc)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }
        return Math.Min(low, _samples.Count - 2);
    }
}
