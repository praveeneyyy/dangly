using System;
using System.Linq;
using System.Windows;
using Hangly.Windows.Physics;
using Xunit;

namespace Hangly.Windows.Tests;

public class RopeSimulationTests
{
    private readonly Vector2D _anchor = new(260, 15);
    private const double Frame120 = 1.0 / 120.0;

    private RopeSimulation MakeRope()
    {
        var rope = new RopeSimulation(RopeConfiguration.Default, _anchor);
        rope.Start();
        return rope;
    }

    private static void Run(RopeSimulation rope, double seconds)
    {
        int steps = (int)(seconds / Frame120);
        for (int i = 0; i < steps; i++)
        {
            rope.Step(Frame120);
        }
    }

    private static double TotalSpeed(RopeSimulation rope)
    {
        return rope.Points.Sum(p => p.Displacement.Magnitude);
    }

    // MARK: - Structure

    [Fact]
    public void SegmentCountMatchesSpecification()
    {
        var rope = MakeRope();
        Assert.Equal(20, rope.Configuration.SegmentCount);
        Assert.Equal(21, rope.Points.Count);
    }

    [Fact]
    public void MassDistributionIsCorrect()
    {
        var rope = MakeRope();

        Assert.True(rope.Points[0].IsPinned);
        Assert.Equal(0.0, rope.Points[0].InverseMass);
        // A heavier charm has a smaller inverse mass than a plain node.
        Assert.True(rope.Points[20].InverseMass < rope.Points[10].InverseMass);
        Assert.Equal(1.0, rope.Points[10].InverseMass);
    }

    [Fact]
    public void AnchorStaysPinned()
    {
        var rope = MakeRope();
        rope.BeginDrag(rope.Points[20].Position);
        rope.UpdateDrag(new Vector2D(4000, -4000), new Vector2D(9000, -9000));
        Run(rope, 2.0);

        Assert.Equal(_anchor, rope.Points[0].Position);
    }

    // MARK: - Inextensibility

    [Fact]
    public void DoesNotStretchAtRest()
    {
        var rope = MakeRope();
        Run(rope, 5.0);

        Assert.True(rope.MeasuredMaximumStretch <= rope.Configuration.MaxStretchRatio + 1e-9);
    }

    [Fact]
    public void DoesNotStretchUnderHardFlick()
    {
        var rope = MakeRope();
        Run(rope, 1.0);
        rope.BeginDrag(rope.Points[20].Position);

        var position = rope.Points[20].Position;
        double worstStretch = 0.0;

        for (int tick = 0; tick < 1200; tick++)
        {
            double direction = ((tick / 18) % 2 == 0) ? 1.0 : -1.0;
            position += new Vector2D(direction * 3000.0 * Frame120, Math.Sin(tick * 0.05) * 12.0);
            rope.UpdateDrag(position, new Vector2D(direction * 3000.0, 0));
            rope.Step(Frame120);
            worstStretch = Math.Max(worstStretch, rope.MeasuredMaximumStretch);
        }

        Assert.True(worstStretch <= rope.Configuration.MaxStretchRatio + 1e-9);
    }

    [Fact]
    public void DoesNotStretchWhileSwinging()
    {
        var rope = MakeRope();
        Run(rope, 1.0);

        double worstStretch = 0.0;
        for (int i = 0; i < 2400; i++)
        {
            rope.Step(Frame120);
            worstStretch = Math.Max(worstStretch, rope.MeasuredMaximumStretch);
        }

        Assert.True(worstStretch < 1.01);
    }

    [Fact]
    public void RecoversFromUnreachableInput()
    {
        var rope = MakeRope();
        Run(rope, 1.0);
        rope.BeginDrag(rope.Points[20].Position);

        double worstStretch = 0.0;
        for (int tick = 0; tick < 600; tick++)
        {
            double angle = tick * 0.35;
            var target = _anchor + (new Vector2D(Math.Cos(angle), Math.Sin(angle)) * 900.0);
            rope.UpdateDrag(target, new Vector2D(6000.0, 6000.0));
            rope.Step(Frame120);
            worstStretch = Math.Max(worstStretch, rope.MeasuredMaximumStretch);
        }
        Assert.True(worstStretch < 1.05);

        rope.EndDrag();
        Run(rope, 1.0);
        Assert.True(rope.MeasuredMaximumStretch <= rope.Configuration.MaxStretchRatio + 1e-9);
    }

    // MARK: - Gravity and Damping

    [Fact]
    public void GravityPullsTheRopeDown()
    {
        var rope = MakeRope();
        double startX = rope.Points[20].Position.X;
        Run(rope, 20.0);
        var charm = rope.Points[20].Position;

        Assert.True(Math.Abs(charm.X - _anchor.X) < Math.Abs(startX - _anchor.X) * 0.25);
        Assert.True(charm.Y > _anchor.Y + (rope.Configuration.TotalLength * 0.9));
    }

    [Fact]
    public void DampingSettlesTheRope()
    {
        var rope = MakeRope();
        Run(rope, 3.0);
        double movingSpeed = TotalSpeed(rope);

        Run(rope, 25.0);
        double settledSpeed = TotalSpeed(rope);

        Assert.True(settledSpeed < movingSpeed * 0.1);
    }

    // MARK: - Frame-Rate Independence

    [Fact]
    public void FixedTimeStepMakesRefreshRateIrrelevant()
    {
        var fast = new RopeSimulation(RopeConfiguration.Default, _anchor);
        var slow = new RopeSimulation(RopeConfiguration.Default, _anchor);
        fast.Start();
        slow.Start();

        for (int i = 0; i < 120; i++)
        {
            fast.Step(1.0 / 120.0);
            fast.Step(1.0 / 120.0);
            slow.Step(1.0 / 60.0);
        }

        for (int index = 0; index < fast.Points.Count; index++)
        {
            Assert.True(Math.Abs(fast.Points[index].Position.X - slow.Points[index].Position.X) < 1e-9);
            Assert.True(Math.Abs(fast.Points[index].Position.Y - slow.Points[index].Position.Y) < 1e-9);
        }
    }

    [Fact]
    public void RemainsStableOverALongRun()
    {
        var rope = MakeRope();
        Run(rope, 120.0);

        double reach = rope.Configuration.TotalLength * 3.0;
        foreach (var point in rope.Points)
        {
            Assert.True(point.Position.IsFinite);
            Assert.True(point.Position.DistanceTo(_anchor) < reach);
        }
    }

    [Fact]
    public void ClampsOversizedFrames()
    {
        var rope = MakeRope();
        rope.Step(10.0);

        int ceiling = (int)(rope.Configuration.MaxFrameDuration / rope.Configuration.FixedTimeStep) + 1;
        Assert.True(rope.LastStepCount <= ceiling);
    }

    // MARK: - Interaction

    [Fact]
    public void GrabIsLimitedToTheCharm()
    {
        var rope = MakeRope();
        Run(rope, 5.0);

        Assert.True(rope.CanGrab(rope.Points[20].Position));
        Assert.False(rope.CanGrab(_anchor));
        Assert.False(rope.CanGrab(rope.Points[10].Position));
        Assert.False(rope.BeginDrag(_anchor));
        Assert.False(rope.IsDragging);
    }

    [Fact]
    public void DraggingMovesTheCharmToTheCursor()
    {
        var rope = MakeRope();
        Run(rope, 5.0);
        rope.BeginDrag(rope.Points[20].Position);

        var target = new Vector2D(_anchor.X + 120.0, _anchor.Y + 140.0);
        for (int i = 0; i < 10; i++)
        {
            rope.UpdateDrag(target, Vector2D.Zero);
            rope.Step(Frame120);
        }

        Assert.True(rope.Points[20].Position.DistanceTo(target) < 1e-6);
    }

    [Fact]
    public void ReleasePreservesMomentum()
    {
        var rope = MakeRope();
        Run(rope, 5.0);
        rope.BeginDrag(rope.Points[20].Position);

        double radius = rope.Configuration.TotalLength * 0.9;
        double speed = 900.0;
        double angularStep = (speed / radius) * Frame120;
        double angle = Math.PI / 2.0;

        for (int i = 0; i < 16; i++)
        {
            angle -= angularStep;
            var target = _anchor + (new Vector2D(Math.Cos(angle), Math.Sin(angle)) * radius);
            var tangent = new Vector2D(Math.Sin(angle), -Math.Cos(angle)) * speed;
            rope.UpdateDrag(target, tangent);
            rope.Step(Frame120);
        }

        var held = rope.Points[20].Displacement / Frame120;
        rope.EndDrag();
        rope.Step(Frame120);
        var released = rope.Points[20].Displacement / Frame120;

        Assert.True(released.Magnitude > held.Magnitude * 0.5);
        Assert.True((released.X * held.X) + (released.Y * held.Y) > 0);
        Assert.True(released.X > 0);
    }

    [Fact]
    public void ReleaseWithoutMotionDoesNotLaunchTheCharm()
    {
        var rope = MakeRope();
        Run(rope, 5.0);
        rope.BeginDrag(rope.Points[20].Position);

        var held = _anchor + (new Vector2D(0.6, 0.8) * (rope.Configuration.TotalLength * 0.9));
        for (int i = 0; i < 60; i++)
        {
            rope.UpdateDrag(held, Vector2D.Zero);
            rope.Step(Frame120);
        }
        rope.EndDrag();
        rope.Step(Frame120);

        double speed = rope.Points[20].Displacement.Magnitude / Frame120;
        Assert.True(speed < 200.0);
    }

    // MARK: - Idling

    [Fact]
    public void SleepsWhenSettled()
    {
        var rope = MakeRope();
        Assert.False(rope.IsSleeping);

        Run(rope, 40.0);

        Assert.True(rope.IsSleeping);
        Assert.Equal(0, rope.LastStepCount);
    }

    [Fact]
    public void SleepingRopeDoesNotDrift()
    {
        var rope = MakeRope();
        Run(rope, 40.0);
        var resting = rope.Points.Select(p => p.Position).ToList();

        Run(rope, 20.0);

        for (int i = 0; i < rope.Points.Count; i++)
        {
            Assert.Equal(resting[i], rope.Points[i].Position);
        }
    }

    [Fact]
    public void DraggingWakesTheRope()
    {
        var rope = MakeRope();
        Run(rope, 40.0);
        Assert.True(rope.IsSleeping);

        rope.BeginDrag(rope.Points[20].Position);

        Assert.False(rope.IsSleeping);
        rope.UpdateDrag(rope.Points[20].Position + new Vector2D(40, 0), new Vector2D(400, 0));
        rope.Step(Frame120);
        Assert.True(rope.LastStepCount > 0);
    }

    [Fact]
    public void ResizingWakesTheRope()
    {
        var rope = MakeRope();
        Run(rope, 40.0);
        Assert.True(rope.IsSleeping);

        rope.Resize(new Size(700, 300));

        Assert.False(rope.IsSleeping);
    }

    // MARK: - Reduce Motion

    [Fact]
    public void RestPoseIsStill()
    {
        var rope = MakeRope();
        rope.ResetToHanging();

        foreach (var point in rope.Points)
        {
            Assert.True(Math.Abs(point.Position.X - _anchor.X) < 1e-9);
            Assert.True(point.Displacement.Magnitude < 1e-9);
        }
        Assert.True(rope.Points[20].Position.Y > _anchor.Y + (rope.Configuration.TotalLength * 0.99));

        Run(rope, 2.0);
        Assert.True(rope.IsSleeping);
        Assert.True(Math.Abs(rope.Points[20].Position.X - _anchor.X) < 0.01);
    }

    // MARK: - Resizing

    [Fact]
    public void ResizingKeepsTheRopeAlive()
    {
        var rope = MakeRope();
        Run(rope, 1.0);

        rope.Resize(new Size(1040, 600));

        Assert.Equal(520.0, rope.Anchor.X);
        Assert.Equal(21, rope.Points.Count);
        Assert.Equal(20, rope.Configuration.SegmentCount);

        Run(rope, 3.0);
        Assert.True(rope.MeasuredMaximumStretch <= rope.Configuration.MaxStretchRatio + 1e-9);
    }
}
