using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Hangly.Windows.Physics;

/// <summary>
/// A hanging rope simulated with Verlet integration and position-based constraints.
/// Exact mathematical port of macOS Hangly's RopeSimulation.swift.
/// </summary>
public sealed class RopeSimulation
{
    private readonly List<RopePoint> _points = [];
    private readonly List<RopeBead> _beads = [];
    private readonly List<CharmBead> _beadDescriptions = [];
    private readonly RopeCurve _curve = new();

    public IReadOnlyList<RopePoint> Points => _points;
    public IReadOnlyList<RopeBead> Beads => _beads;
    public IReadOnlyList<CharmBead> BeadDescriptions => _beadDescriptions;
    public RopeCurve Curve => _curve;

    public RopeConfiguration Configuration { get; private set; }
    public Vector2D Anchor { get; private set; }
    public CharmMetrics CharmMetrics { get; private set; }

    public Vector2D CordEnd { get; private set; } = Vector2D.Zero;
    public double CordLength { get; private set; } = 0.0;
    public double CharmOrientation { get; private set; } = Math.PI / 2.0;

    public bool IsRunning { get; private set; }
    public int LastStepCount { get; private set; }
    public bool IsSleeping { get; private set; }

    private int _stillFrames;
    private double _accumulator;

    public int? DragIndex { get; private set; }
    public Vector2D DragTarget { get; private set; } = Vector2D.Zero;
    public Vector2D DragVelocity { get; private set; } = Vector2D.Zero;

    public const double BeadTetherStiffness = 0.05;
    public const int BeadSeparationPasses = 2;

    public RopeSimulation(
        RopeConfiguration configuration = default,
        Vector2D anchor = default,
        CharmMetrics charmMetrics = default)
    {
        Configuration = configuration.SegmentCount == 0 ? RopeConfiguration.Default : configuration;
        Anchor = anchor;
        CharmMetrics = charmMetrics.Mass == 0 ? CharmMetrics.Default : charmMetrics;
        Reset();
    }

    public void SetCharmMetrics(CharmMetrics metrics)
    {
        if (metrics.Equals(CharmMetrics)) return;
        CharmMetrics = metrics;
        RebuildBeads(preservingMotion: true);
        Wake();
    }

    public void SetBeads(IEnumerable<CharmBead> descriptions)
    {
        var list = descriptions.ToList();
        if (_beadDescriptions.SequenceEqual(list)) return;
        _beadDescriptions.Clear();
        _beadDescriptions.AddRange(list);
        RebuildBeads(preservingMotion: false);
        Wake();
    }

    public bool IsDragging => DragIndex.HasValue;

    public void Start()
    {
        if (IsRunning) return;
        if (_points.Count == 0) Reset();
        _accumulator = 0.0;
        IsRunning = true;
        Wake();
    }

    public void Stop()
    {
        IsRunning = false;
        _accumulator = 0.0;
    }

    public void Step(double deltaTime)
    {
        if (!IsRunning || deltaTime <= 0 || IsSleeping)
        {
            LastStepCount = 0;
            return;
        }

        _accumulator = Math.Min(_accumulator + deltaTime, Configuration.MaxFrameDuration);

        double timeStep = Configuration.FixedTimeStep;
        int taken = 0;
        while (_accumulator >= timeStep)
        {
            Advance(timeStep);
            _accumulator -= timeStep;
            taken++;
        }
        LastStepCount = taken;
        UpdateSleepState();
    }

    public void Wake()
    {
        IsSleeping = false;
        _stillFrames = 0;
    }

    private void UpdateSleepState()
    {
        if (DragIndex.HasValue)
        {
            _stillFrames = 0;
            return;
        }

        double speedLimit = Configuration.RestSpeed * Configuration.FixedTimeStep;
        bool moving = _points.Any(p => p.Displacement.Magnitude > speedLimit)
                   || _beads.Any(b => b.Displacement.Magnitude > speedLimit);
        if (moving)
        {
            _stillFrames = 0;
            return;
        }

        _stillFrames++;
        if (_stillFrames >= Configuration.FramesBeforeSleep)
        {
            IsSleeping = true;
        }
    }

    public void Reset()
    {
        Reset(Configuration.InitialAngle);
    }

    public void ResetToHanging()
    {
        Reset(0.0);
    }

    private void Reset(double angle)
    {
        _points.Clear();
        _points.AddRange(RopePoint.CreateChain(Configuration, Anchor, CharmMetrics, angle));
        _accumulator = 0.0;
        DragIndex = null;
        DragVelocity = Vector2D.Zero;
        LastStepCount = 0;
        RebuildBeads(preservingMotion: false);
        Wake();
    }

    public void Resize(Size canvasSize)
    {
        var fitted = RopeConfiguration.Fitted(canvasSize);
        bool needsRebuild = _points.Count != fitted.PointCount;

        Configuration = fitted;
        Anchor = RopeConfiguration.Layout.Anchor(canvasSize);

        if (needsRebuild)
        {
            Reset();
        }
        else
        {
            RebuildBeads(preservingMotion: true);
            Wake();
        }
    }

    private void Advance(double timeStep)
    {
        EnforceAnchor();
        Integrate(timeStep);
        DriveDraggedPoint(timeStep);

        int relaxations = 0;
        double residual = double.PositiveInfinity;
        while (relaxations < Configuration.ConstraintIterations &&
               residual >= Configuration.ConvergenceTolerance)
        {
            residual = SolveDistanceConstraints();
            relaxations++;
        }

        EnforceMaximumStretch();
        RefreshCord();
        AdvanceBeads(timeStep);
    }

    private void EnforceAnchor()
    {
        if (_points.Count == 0) return;
        var p0 = _points[0];
        p0.Position = Anchor;
        p0.PreviousPosition = Anchor;
        _points[0] = p0;
    }

    private void Integrate(double timeStep)
    {
        var gravityStep = new Vector2D(0, Configuration.Gravity * timeStep * timeStep);
        double damping = Configuration.Damping;
        double displacementLimit = Configuration.MaximumSpeed * timeStep;

        for (int index = 0; index < _points.Count; index++)
        {
            if (index == DragIndex) continue;
            if (_points[index].InverseMass <= 0.0) continue;

            var point = _points[index];
            var carried = (point.Displacement * damping).LimitedTo(displacementLimit);
            point.PreviousPosition = point.Position;
            point.Position += carried + gravityStep;
            _points[index] = point;
        }
    }

    private void DriveDraggedPoint(double timeStep)
    {
        if (!DragIndex.HasValue) return;

        int index = DragIndex.Value;
        var current = _points[index].Position;
        double travelLimit = Configuration.MaximumSpeed * timeStep;
        var p = _points[index];
        p.Position = current + (DragTarget - current).LimitedTo(travelLimit);
        p.SetVelocity(DragVelocity, timeStep);
        _points[index] = p;
    }

    public double SolveDistanceConstraints()
    {
        double restLength = Configuration.SegmentLength;
        double largestCorrection = 0.0;
        for (int index = 0; index < _points.Count - 1; index++)
        {
            double correction = SolveLink(index, index + 1, restLength);
            largestCorrection = Math.Max(largestCorrection, correction);
        }
        return largestCorrection;
    }

    private double SolveLink(int indexA, int indexB, double restLength)
    {
        double inverseA = EffectiveInverseMass(indexA);
        double inverseB = EffectiveInverseMass(indexB);
        double totalInverseMass = inverseA + inverseB;
        if (totalInverseMass <= 0.0) return 0.0;

        var delta = _points[indexB].Position - _points[indexA].Position;
        double distance = delta.Magnitude;
        if (distance <= double.Epsilon) return 0.0;

        var correction = delta * ((distance - restLength) / distance / totalInverseMass);
        var pA = _points[indexA];
        var pB = _points[indexB];
        pA.Position += correction * inverseA;
        pB.Position -= correction * inverseB;
        _points[indexA] = pA;
        _points[indexB] = pB;

        return Math.Max((correction * inverseA).Magnitude, (correction * inverseB).Magnitude);
    }

    private void EnforceMaximumStretch()
    {
        double limit = Configuration.SegmentLength * Configuration.MaxStretchRatio;

        for (int pass = 0; pass < Configuration.StretchPasses; pass++)
        {
            bool corrected = false;
            for (int index = 0; index < _points.Count - 1; index++)
            {
                if (ClampLink(index, limit))
                {
                    corrected = true;
                }
            }

            if (!corrected) return;
        }
    }

    private bool ClampLink(int index, double limit)
    {
        int lower = index;
        int upper = index + 1;

        double inverseLower = EffectiveInverseMass(lower);
        double inverseUpper = EffectiveInverseMass(upper);
        double totalInverseMass = inverseLower + inverseUpper;
        if (totalInverseMass <= 0.0) return false;

        var delta = _points[upper].Position - _points[lower].Position;
        double distance = delta.Magnitude;
        if (distance <= limit || distance <= double.Epsilon) return false;

        var correction = delta * ((distance - limit) / distance / totalInverseMass);
        var pLower = _points[lower];
        var pUpper = _points[upper];
        pLower.Position += correction * inverseLower;
        pUpper.Position -= correction * inverseUpper;
        _points[lower] = pLower;
        _points[upper] = pUpper;
        return true;
    }

    private double EffectiveInverseMass(int index)
    {
        return index == DragIndex ? 0.0 : _points[index].InverseMass;
    }

    public void SetInverseMass(double value, int index)
    {
        if (index < 0 || index >= _points.Count) return;
        var p = _points[index];
        p.InverseMass = value;
        _points[index] = p;
    }

    // MARK: - Dragging

    public bool BeginDrag(Vector2D location)
    {
        if (_points.Count == 0) return false;
        if (!CanGrab(location)) return false;

        DragIndex = _points.Count - 1;
        DragTarget = location;
        DragVelocity = Vector2D.Zero;
        Wake();
        return true;
    }

    public bool CanGrab(Vector2D location)
    {
        if (_points.Count == 0) return false;
        double radius = CharmRadius + RopeConfiguration.Layout.GrabPadding;
        return _points[^1].Position.DistanceTo(location) <= radius;
    }

    public void UpdateDrag(Vector2D location, Vector2D velocity)
    {
        if (!DragIndex.HasValue) return;
        DragTarget = ReachableTarget(location);
        DragVelocity = velocity.LimitedTo(Configuration.MaximumSpeed);
    }

    public Vector2D ReachableTarget(Vector2D location)
    {
        double reach = Configuration.TotalLength * Configuration.MaximumReachRatio;
        var offset = location - Anchor;
        double distance = offset.Magnitude;
        if (distance > reach && distance > double.Epsilon)
        {
            return Anchor + ((offset / distance) * reach);
        }
        return location;
    }

    public void EndDrag()
    {
        DragIndex = null;
        DragVelocity = Vector2D.Zero;
    }

    // MARK: - Beads & Cord

    public void AdvanceBeads(double timeStep)
    {
        if (_beads.Count == 0 || _curve.IsEmpty) return;

        var gravityStep = new Vector2D(0, Configuration.Gravity * timeStep * timeStep);
        double damping = Configuration.Damping;
        double displacementLimit = Configuration.MaximumSpeed * timeStep;
        double drawnLength = CordLength;

        for (int index = 0; index < _beads.Count; index++)
        {
            var bead = _beads[index];
            var carried = (bead.Displacement * damping).LimitedTo(displacementLimit);
            var predicted = bead.Position + carried + gravityStep;

            double rest = Math.Clamp(drawnLength - bead.RestOffset, 0.0, drawnLength);
            double window = bead.SlideLimit + bead.SpacingRadius + Configuration.SegmentLength;
            double arc = _curve.ArcNearestTo(predicted, bead.Arc, window);
            arc += (rest - arc) * BeadTetherStiffness;
            bead.Arc = Math.Clamp(arc, rest - bead.SlideLimit, rest + bead.SlideLimit);
            _beads[index] = bead;
        }

        SeparateBeads(drawnLength);

        for (int index = 0; index < _beads.Count; index++)
        {
            var bead = _beads[index];
            bead.PreviousPosition = bead.Position;
            bead.Position = _curve.PointAtArc(bead.Arc);
            bead.Angle = _curve.AngleAtArc(bead.Arc);
            _beads[index] = bead;
        }
    }

    public void SeparateBeads(double cordLength)
    {
        if (_beads.Count == 0) return;
        int last = _beads.Count - 1;

        for (int pass = 0; pass < BeadSeparationPasses; pass++)
        {
            var lastBead = _beads[last];
            lastBead.Arc = Math.Min(lastBead.Arc, cordLength - lastBead.SpacingRadius);
            _beads[last] = lastBead;

            if (last == 0) return;

            for (int index = last - 1; index >= 0; index--)
            {
                double minimumGap = _beads[index].SpacingRadius + _beads[index + 1].SpacingRadius;
                double gap = _beads[index + 1].Arc - _beads[index].Arc;
                if (gap < minimumGap)
                {
                    var b = _beads[index];
                    b.Arc -= minimumGap - gap;
                    _beads[index] = b;
                }
            }

            var firstBead = _beads[0];
            firstBead.Arc = Math.Max(firstBead.Arc, firstBead.SpacingRadius);
            _beads[0] = firstBead;
        }
    }

    public void RefreshCord()
    {
        if (_points.Count < 2) return;

        var pts = new List<Vector2D>(_points.Count);
        for (int i = 0; i < _points.Count; i++)
        {
            pts.Add(_points[i].Position);
        }

        _curve.Rebuild(pts, CharmCenter);
        CordLength = _curve.ArcEnteringCircleAround(CharmCenter, KnotDistance);
        CordEnd = _curve.PointAtArc(CordLength);

        var delta = CharmCenter - CordEnd;
        if (delta.MagnitudeSquared > double.Epsilon)
        {
            CharmOrientation = Math.Atan2(delta.Y, delta.X);
        }
    }

    public double KnotDistance => CharmRadius * CharmMetrics.KnotInset;

    public void RebuildBeads(bool preservingMotion)
    {
        double radius = CharmRadius;
        RefreshCord();

        if (_beadDescriptions.Count == 0 || radius <= 0 || _points.Count < 2)
        {
            _beads.Clear();
            ApplyMasses();
            return;
        }

        double drawnLength = _curve.IsEmpty ? Configuration.TotalLength - KnotDistance : CordLength;

        var newBeads = new List<RopeBead>(_beadDescriptions.Count);
        for (int index = 0; index < _beadDescriptions.Count; index++)
        {
            var description = _beadDescriptions[index];
            double restOffset = description.Offset * radius;
            double arc = Math.Clamp(drawnLength - restOffset, 0.0, Math.Max(drawnLength, 0.0));
            RopeBead? existing = (preservingMotion && index < _beads.Count) ? _beads[index] : null;
            var position = existing.HasValue ? existing.Value.Position : _curve.PointAtArc(arc);
            var prevPos = existing.HasValue ? existing.Value.PreviousPosition : position;
            double beadArc = existing.HasValue ? existing.Value.Arc : arc;
            double angle = existing.HasValue ? existing.Value.Angle : _curve.AngleAtArc(arc);

            newBeads.Add(new RopeBead(
                position: position,
                previousPosition: prevPos,
                arc: beadArc,
                restOffset: restOffset,
                spacingRadius: description.SpacingRatio * radius,
                size: new Vector2D(description.Size.X * radius, description.Size.Y * radius),
                mass: description.Mass,
                angle: angle
            ));
        }

        _beads.Clear();
        _beads.AddRange(newBeads);
        ApplyMasses();
    }

    public void ApplyMasses()
    {
        if (_points.Count <= 1) return;
        int last = _points.Count - 1;

        for (int index = 0; index < _points.Count; index++)
        {
            if (index == 0)
            {
                SetInverseMass(0.0, 0);
            }
            else if (index == last)
            {
                SetInverseMass(1.0 / Math.Max(CharmMetrics.Mass, 0.0001), last);
            }
            else
            {
                SetInverseMass(1.0, index);
            }
        }

        if (_beads.Count == 0 || Configuration.SegmentLength <= double.Epsilon) return;

        double[] load = new double[_points.Count];
        foreach (var bead in _beads)
        {
            double position = Math.Clamp(bead.Arc / Configuration.SegmentLength, 0.0, (double)last);
            int lower = (int)position;
            int upper = Math.Min(lower + 1, last);
            double fraction = position - lower;
            load[lower] += bead.Mass * (1.0 - fraction);
            load[upper] += bead.Mass * fraction;
        }

        for (int index = 1; index <= last; index++)
        {
            if (load[index] > 0.0)
            {
                double baseMass = (index == last) ? CharmMetrics.Mass : 1.0;
                SetInverseMass(1.0 / Math.Max(baseMass + load[index], 0.0001), index);
            }
        }
    }

    // MARK: - Snapshot & Metrics

    public double CharmRadius => Configuration.TotalLength * CharmMetrics.RadiusRatio;

    public double CharmAngle => CharmOrientation;

    public Vector2D CharmCenter => _points.Count > 0 ? _points[^1].Position : Vector2D.Zero;

    public double MeasuredMaximumStretch
    {
        get
        {
            if (Configuration.SegmentLength <= double.Epsilon || _points.Count < 2) return 1.0;
            double longest = 0.0;
            for (int index = 0; index < _points.Count - 1; index++)
            {
                double distance = _points[index].Position.DistanceTo(_points[index + 1].Position);
                longest = Math.Max(longest, distance / Configuration.SegmentLength);
            }
            return longest;
        }
    }

    public RopeSnapshot Snapshot()
    {
        var pts = _points.Select(p => p.Position).ToList();
        var beadPlacements = _beads.Select(b => new BeadPlacement(b.Position, b.Angle, b.Size)).ToList();
        return new RopeSnapshot(
            Points: pts,
            CharmRadius: CharmRadius,
            CharmAngle: CharmAngle,
            CharmKnotInset: CharmMetrics.KnotInset,
            Beads: beadPlacements,
            MaximumStretch: MeasuredMaximumStretch,
            IsDragging: IsDragging
        );
    }
}
