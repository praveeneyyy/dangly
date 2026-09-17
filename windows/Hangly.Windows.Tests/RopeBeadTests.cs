using System;
using System.Linq;
using System.Windows;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Physics;
using Xunit;

namespace Hangly.Windows.Tests;

public class RopeBeadTests
{
    private readonly Size _size = new(740, 420);
    private const double Frame120 = 1.0 / 120.0;

    private (RopeSimulation Rope, ICharm Charm) MakeRope(CharmKind kind = CharmKind.Daruma)
    {
        var charm = BuiltInCharms.Get(kind);
        var rope = new RopeSimulation(
            configuration: RopeConfiguration.Fitted(_size),
            anchor: RopeConfiguration.Layout.Anchor(_size),
            charmMetrics: charm.Metrics
        );
        rope.Start();
        rope.SetCharmMetrics(charm.Metrics);
        rope.SetBeads(charm.Beads);
        return (rope, charm);
    }

    private static void Run(RopeSimulation rope, double seconds)
    {
        int steps = (int)(seconds / Frame120);
        for (int i = 0; i < steps; i++)
        {
            rope.Step(Frame120);
        }
    }

    private static void Shake(RopeSimulation rope, int cycles = 6)
    {
        rope.BeginDrag(rope.Points[20].Position);
        double angle = Math.PI / 2.0;
        double radius = rope.Configuration.TotalLength * 0.9;
        for (int tick = 0; tick < cycles * 60; tick++)
        {
            angle += ((tick / 30) % 2 == 0) ? 0.08 : -0.08;
            var target = rope.Anchor + (new Vector2D(Math.Cos(angle), Math.Sin(angle)) * radius);
            rope.UpdateDrag(target, new Vector2D(2200, 1200));
            rope.Step(Frame120);
        }
        rope.EndDrag();
    }

    private static RopeCurve GetCurve(RopeSimulation rope)
    {
        return new RopeCurve(rope.Points.Select(p => p.Position).ToList(), rope.Points[20].Position);
    }

    // MARK: - Placement

    [Fact]
    public void BeadsComeFromTheArtwork()
    {
        var charm = BuiltInCharms.Get(CharmKind.Daruma);
        Assert.Equal(3, charm.Beads.Count);

        double previous = double.PositiveInfinity;
        foreach (var bead in charm.Beads)
        {
            Assert.True(bead.Offset > 0, "a bead must sit above the knot");
            Assert.True(bead.Offset < previous);
            Assert.True(bead.Size.X > 0);
            Assert.True(bead.Size.Y > 0);
            Assert.True(bead.Mass > 0);
            previous = bead.Offset;
        }

        Assert.Empty(BuiltInCharms.Get(CharmKind.Himmeli).Beads);
        Assert.Empty(BuiltInCharms.Get(CharmKind.Circle).Beads);
    }

    [Fact]
    public void BeadsSitOnTheCord()
    {
        var (rope, _) = MakeRope();
        Run(rope, 4.0);
        var line = GetCurve(rope);

        Assert.Equal(3, rope.Beads.Count);
        foreach (var bead in rope.Beads)
        {
            double arc = line.ArcNearestTo(bead.Position, bead.Arc, 200.0);
            Assert.True(line.PointAtArc(arc).DistanceTo(bead.Position) < 0.5);
            Assert.True(bead.Arc <= rope.CordLength + 0.001, "a bead must not pass the knot");
            Assert.True(bead.Position.DistanceTo(rope.Points[20].Position) > rope.CharmRadius * 0.9);
        }
    }

    [Fact]
    public void BeadsNeverOverlap()
    {
        var (rope, _) = MakeRope();
        Run(rope, 1.0);

        double worstOverlap = 0.0;
        for (int c = 0; c < 6; c++)
        {
            Shake(rope, cycles: 1);
            for (int i = 0; i < rope.Beads.Count - 1; i++)
            {
                double gap = rope.Beads[i + 1].Arc - rope.Beads[i].Arc;
                double needed = rope.Beads[i].SpacingRadius + rope.Beads[i + 1].SpacingRadius;
                worstOverlap = Math.Max(worstOverlap, needed - gap);
            }
            var last = rope.Beads[^1];
            worstOverlap = Math.Max(worstOverlap, (last.Arc + last.SpacingRadius) - rope.CordLength);
        }
        Assert.True(worstOverlap < 0.001);
    }

    [Fact]
    public void BeadsSlideButDoNotMigrate()
    {
        var (rope, _) = MakeRope();
        Run(rope, 4.0);
        var resting = rope.Beads.Select(b => b.Arc).ToList();

        double Slide(RopeBead bead) => Math.Abs((rope.CordLength - bead.Arc) - bead.RestOffset);

        double largestSlide = 0.0;
        rope.BeginDrag(rope.Points[20].Position);
        for (int tick = 0; tick < 240; tick++)
        {
            var target = rope.Anchor + new Vector2D(Math.Sin(tick * 0.25) * 240.0, 210.0);
            rope.UpdateDrag(target, new Vector2D(2600.0, 0));
            rope.Step(Frame120);
            foreach (var bead in rope.Beads)
            {
                largestSlide = Math.Max(largestSlide, Slide(bead));
                Assert.True(Slide(bead) <= bead.SlideLimit + bead.SpacingRadius + 1e-6);
            }
        }
        rope.EndDrag();

        Assert.True(largestSlide > 0.5);

        Run(rope, 20.0);
        for (int i = 0; i < rope.Beads.Count; i++)
        {
            Assert.True(Math.Abs(rope.Beads[i].Arc - resting[i]) < 2.0, $"bead {i} drifted");
        }
    }

    [Fact]
    public void BeadsFollowTheRope()
    {
        var (rope, _) = MakeRope();
        Run(rope, 4.0);
        var before = rope.Beads.Select(b => b.Position).ToList();

        rope.BeginDrag(rope.Points[20].Position);
        var target = rope.Anchor + new Vector2D(220, 180);
        for (int i = 0; i < 60; i++)
        {
            rope.UpdateDrag(target, new Vector2D(1800, 0));
            rope.Step(Frame120);
        }

        for (int i = 0; i < rope.Beads.Count; i++)
        {
            Assert.True(rope.Beads[i].Position.DistanceTo(before[i]) > 10.0, $"bead {i} did not travel");
        }
    }

    [Fact]
    public void BeadsLoadTheRope()
    {
        var charm = BuiltInCharms.Get(CharmKind.Daruma);
        var bare = new RopeSimulation(
            configuration: RopeConfiguration.Fitted(_size),
            anchor: RopeConfiguration.Layout.Anchor(_size),
            charmMetrics: charm.Metrics
        );
        bare.Start();
        var (loaded, _) = MakeRope();

        var lightened = bare.Points.Zip(loaded.Points).Where(pair => pair.First.InverseMass > pair.Second.InverseMass);
        Assert.NotEmpty(lightened);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(bare.Points[i].InverseMass, loaded.Points[i].InverseMass);
        }
    }

    [Fact]
    public void BeadsDoNotDisturbTheRope()
    {
        var (rope, _) = MakeRope();
        Run(rope, 1.0);

        double worstStretch = 0.0;
        Shake(rope, cycles: 4);
        for (int i = 0; i < 1200; i++)
        {
            rope.Step(Frame120);
            worstStretch = Math.Max(worstStretch, rope.MeasuredMaximumStretch);
        }
        Assert.True(worstStretch <= rope.Configuration.MaxStretchRatio + 1e-9);

        foreach (var bead in rope.Beads)
        {
            Assert.True(double.IsFinite(bead.Position.X));
            Assert.True(double.IsFinite(bead.Position.Y));
        }

        Run(rope, 40.0);
        Assert.True(rope.IsSleeping, "beads must not keep the overlay awake");
    }

    [Fact]
    public void BeadsAreFrameRateIndependent()
    {
        var (fast, _) = MakeRope();
        var (slow, _) = MakeRope();

        for (int i = 0; i < 120; i++)
        {
            fast.Step(1.0 / 120.0);
            fast.Step(1.0 / 120.0);
            slow.Step(1.0 / 60.0);
        }

        for (int i = 0; i < fast.Beads.Count; i++)
        {
            Assert.True(Math.Abs(fast.Beads[i].Arc - slow.Beads[i].Arc) < 1e-9);
        }
    }

    [Fact]
    public void BeadsFollowTheCharm()
    {
        var (rope, _) = MakeRope(CharmKind.Daruma);
        Run(rope, 2.0);
        Assert.Equal(3, rope.Beads.Count);

        var himmeli = BuiltInCharms.Get(CharmKind.Himmeli);
        rope.SetCharmMetrics(himmeli.Metrics);
        rope.SetBeads(himmeli.Beads);
        Assert.Empty(rope.Beads);

        var ghanta = BuiltInCharms.Get(CharmKind.Ghanta);
        rope.SetCharmMetrics(ghanta.Metrics);
        rope.SetBeads(ghanta.Beads);
        Run(rope, 2.0);
        Assert.Equal(ghanta.Beads.Count, rope.Beads.Count);
        Assert.True(rope.Beads[0].Arc <= rope.CordLength);
    }

    [Fact]
    public void BeadsScaleWithTheOverlay()
    {
        var (rope, _) = MakeRope();
        Run(rope, 3.0);
        var before = rope.Beads.Select(b => b.Size.Y / rope.CharmRadius).ToList();

        rope.Resize(new Size(_size.Width * 1.5, _size.Height * 1.5));
        Run(rope, 3.0);
        var after = rope.Beads.Select(b => b.Size.Y / rope.CharmRadius).ToList();

        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.True(Math.Abs(before[i] - after[i]) < 1e-9);
        }
        Assert.True(rope.Beads[0].Arc <= rope.CordLength);
    }
}
