using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Physics;

namespace Hangly.Windows.Rendering;

/// <summary>
/// Immediate-mode WPF renderer for the simulated rope, twisted cord, beads, ambient glow and charm.
/// Faithfully translates macOS Hangly's RopeCanvasView.swift and CharmRenderer.swift.
/// </summary>
public sealed class RopeCanvas : FrameworkElement
{
    private RopeSnapshot _snapshot = new([], 0, 0, 0.9, [], 1.0, false);
    private ICharm _activeCharm = BuiltInCharms.Get(CharmKind.Circle);
    private bool _isDropTargeted;
    private double? _importSpinnerAngle;

    public RopeSnapshot Snapshot
    {
        get => _snapshot;
        set
        {
            _snapshot = value;
            InvalidateVisual();
        }
    }

    public ICharm ActiveCharm
    {
        get => _activeCharm;
        set
        {
            _activeCharm = value;
            InvalidateVisual();
        }
    }

    public bool IsDropTargeted
    {
        get => _isDropTargeted;
        set
        {
            if (_isDropTargeted != value)
            {
                _isDropTargeted = value;
                InvalidateVisual();
            }
        }
    }

    public double? ImportSpinnerAngle
    {
        get => _importSpinnerAngle;
        set
        {
            if (_importSpinnerAngle != value)
            {
                _importSpinnerAngle = value;
                InvalidateVisual();
            }
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_snapshot.Points.Count < 2) return;

        var cordPalette = _activeCharm.CordTint ?? _activeCharm.Palette;
        var curve = new RopeCurve(_snapshot.Points, _snapshot.Points[^1]);
        double drawnCordLength = curve.ArcEnteringCircleAround(
            _snapshot.Points[^1],
            _snapshot.CharmRadius * _snapshot.CharmKnotInset
        );

        DrawRope(dc, curve, drawnCordLength, cordPalette);
        DrawBeads(dc, cordPalette);
        DrawAmbientGlow(dc, _snapshot.Points[^1], _snapshot.CharmRadius, _activeCharm.Palette);
        DrawCharm(dc, _snapshot.Points[^1], _snapshot.CharmRadius, _snapshot.CharmAngle);
        DrawKnot(dc, curve, drawnCordLength, _snapshot.CharmRadius, _snapshot.CharmAngle, cordPalette);
        DrawActivity(dc, _snapshot.Points[^1], _snapshot.CharmRadius);
    }

    private void DrawRope(DrawingContext dc, RopeCurve curve, double drawnCordLength, CharmPalette palette)
    {
        var polyline = curve.Polyline(drawnCordLength);
        if (polyline.Count < 2) return;

        double width = Math.Max(1.5, _snapshot.CharmRadius * 0.046);

        // Build StreamGeometry from polyline
        var ropeGeo = new StreamGeometry();
        using (var ctx = ropeGeo.Open())
        {
            ctx.BeginFigure(polyline[0], isFilled: false, isClosed: false);
            for (int i = 1; i < polyline.Count; i++)
            {
                ctx.LineTo(polyline[i], isStroked: true, isSmoothJoin: true);
            }
        }
        ropeGeo.Freeze();

        // 1. Soft shadows offset downward
        var shadowPen1 = new Pen(new SolidColorBrush(Color.FromArgb(25, 0, 0, 0)), width * 2.6)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        shadowPen1.Freeze();

        var shadowPen2 = new Pen(new SolidColorBrush(Color.FromArgb(35, 0, 0, 0)), width * 1.5)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        shadowPen2.Freeze();

        dc.PushTransform(new TranslateTransform(0, width * 0.8));
        dc.DrawGeometry(null, shadowPen1, ropeGeo);
        dc.DrawGeometry(null, shadowPen2, ropeGeo);
        dc.Pop();

        // 2. Core cord with linear gradient
        var coreBrush = new LinearGradientBrush(
            palette.Secondary.WithAlpha(0.65).ToMediaColor(),
            palette.Primary.ToMediaColor(),
            (Point)_snapshot.Points[0],
            (Point)_snapshot.Points[^1]
        )
        {
            MappingMode = BrushMappingMode.Absolute
        };
        coreBrush.Freeze();

        var corePen = new Pen(coreBrush, width)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        corePen.Freeze();
        dc.DrawGeometry(null, corePen, ropeGeo);

        // 3. Twisted spiral cord dashed strokes
        if (width > 1.4)
        {
            double pitch = width * 1.5;
            double across = width * 0.20;

            // Highlight strand
            var twistLightPen = new Pen(
                new SolidColorBrush(palette.Light.WithAlpha(0.35).ToMediaColor()),
                width * 0.55)
            {
                DashStyle = new DashStyle([pitch * 0.42 / width, pitch * 0.58 / width], 0)
            };
            twistLightPen.Freeze();

            dc.PushTransform(new TranslateTransform(-across, -across));
            dc.DrawGeometry(null, twistLightPen, ropeGeo);
            dc.Pop();

            // Shade strand
            var twistDeepPen = new Pen(
                new SolidColorBrush(palette.Deep.WithAlpha(0.45).ToMediaColor()),
                width * 0.45)
            {
                DashStyle = new DashStyle([pitch * 0.34 / width, pitch * 0.66 / width], pitch * 0.5 / width)
            };
            twistDeepPen.Freeze();

            dc.PushTransform(new TranslateTransform(across, across));
            dc.DrawGeometry(null, twistDeepPen, ropeGeo);
            dc.Pop();
        }

        // 4. Highlight along edge
        var highlightPen = new Pen(
            new SolidColorBrush(palette.Light.WithAlpha(0.40).ToMediaColor()),
            Math.Max(0.75, width * 0.3))
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        highlightPen.Freeze();

        dc.PushTransform(new TranslateTransform(-width * 0.18, -width * 0.18));
        dc.DrawGeometry(null, highlightPen, ropeGeo);
        dc.Pop();
    }

    private void DrawBeads(DrawingContext dc, CharmPalette cordPalette)
    {
        if (_snapshot.Beads.Count == 0) return;

        for (int i = 0; i < _snapshot.Beads.Count; i++)
        {
            var bead = _snapshot.Beads[i];
            double w = bead.Size.X;
            double h = bead.Size.Y;
            if (w <= 0 || h <= 0) continue;

            dc.PushTransform(new TranslateTransform(bead.Position.X, bead.Position.Y));
            dc.PushTransform(new RotateTransform((bead.Angle - Math.PI / 2.0) * 180.0 / Math.PI));

            if (_activeCharm.DrawBead(dc, i, w, h))
            {
                dc.Pop();
                dc.Pop();
                continue;
            }

            var beadBrush = new RadialGradientBrush(
                cordPalette.Light.ToMediaColor(),
                cordPalette.Primary.ToMediaColor())
            {
                GradientOrigin = new Point(0.35, 0.3),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            beadBrush.Freeze();

            var beadStroke = new Pen(cordPalette.Deep.ToBrush(), Math.Max(0.5, h * 0.08));
            beadStroke.Freeze();

            dc.DrawEllipse(beadBrush, beadStroke, new Point(0, 0), w * 0.5, h * 0.5);

            // Specular bead glint
            var glintBrush = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255));
            glintBrush.Freeze();
            dc.DrawEllipse(glintBrush, null, new Point(-w * 0.16, -h * 0.16), w * 0.15, h * 0.12);

            dc.Pop();
            dc.Pop();
        }
    }

    private static void DrawAmbientGlow(DrawingContext dc, Vector2D center, double radius, CharmPalette palette)
    {
        if (radius <= 2.0) return;
        double haloRadius = radius * 1.7;

        var tint = palette.Primary;
        var glowBrush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops =
            [
                new GradientStop(tint.WithAlpha(0.20).ToMediaColor(), 0.4),
                new GradientStop(tint.WithAlpha(0.08).ToMediaColor(), 0.7),
                new GradientStop(tint.WithAlpha(0.00).ToMediaColor(), 1.0)
            ]
        };
        glowBrush.Freeze();

        dc.DrawEllipse(glowBrush, null, (Point)center, haloRadius, haloRadius);
    }

    private void DrawCharm(DrawingContext dc, Vector2D center, double radius, double charmAngle)
    {
        double side = radius * 2.0;
        if (side <= 0) return;

        double rotationAngleDegrees = (charmAngle - Math.PI / 2.0) * 180.0 / Math.PI;

        dc.PushTransform(new TranslateTransform(center.X, center.Y));
        dc.PushTransform(new RotateTransform(rotationAngleDegrees));
        dc.PushTransform(new TranslateTransform(-radius, -radius));

        _activeCharm.DrawBody(dc, side);

        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private void DrawKnot(DrawingContext dc, RopeCurve curve, double drawnCordLength, double radius, double charmAngle, CharmPalette palette)
    {
        if (radius <= 1.0) return;
        if (_activeCharm is CollectionCharm) return; // Collection SVGs draw their own loop

        var end = curve.PointAtArc(drawnCordLength);
        double ringRadius = radius * 0.18;

        var pen = new Pen(new SolidColorBrush(palette.Light.WithAlpha(0.90).ToMediaColor()), Math.Max(1.0, radius * 0.07));
        pen.Freeze();

        dc.DrawEllipse(null, pen, (Point)end, ringRadius, ringRadius);
    }

    private void DrawActivity(DrawingContext dc, Vector2D center, double radius)
    {
        double haloRadius = radius * 1.45;
        if (haloRadius <= 2.0) return;

        if (_importSpinnerAngle.HasValue)
        {
            // Turning arc while importing
            double startAngle = _importSpinnerAngle.Value;
            double endAngle = startAngle + (Math.PI * 1.4);

            var arcGeo = new StreamGeometry();
            using (var ctx = arcGeo.Open())
            {
                var startPt = new Point(center.X + Math.Cos(startAngle) * haloRadius, center.Y + Math.Sin(startAngle) * haloRadius);
                var endPt = new Point(center.X + Math.Cos(endAngle) * haloRadius, center.Y + Math.Sin(endAngle) * haloRadius);
                ctx.BeginFigure(startPt, isFilled: false, isClosed: false);
                ctx.ArcTo(endPt, new Size(haloRadius, haloRadius), 0, isLargeArc: true, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
            }
            arcGeo.Freeze();

            var spinnerPen = new Pen(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), 3.0)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            spinnerPen.Freeze();
            dc.DrawGeometry(null, spinnerPen, arcGeo);
        }
        else if (_isDropTargeted)
        {
            // Dashed ring when file hovers
            var fillBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            fillBrush.Freeze();

            var ringPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), 2.0)
            {
                DashStyle = new DashStyle([3, 2.5], 0)
            };
            ringPen.Freeze();

            dc.DrawEllipse(fillBrush, ringPen, (Point)center, haloRadius, haloRadius);
        }
    }
}
