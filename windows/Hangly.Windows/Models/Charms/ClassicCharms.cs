using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Hangly.Windows.Physics;

namespace Hangly.Windows.Models.Charms;

public sealed class CircleCharm : ICharm
{
    public CharmID Id => CharmID.FromBuiltIn(CharmKind.Circle);
    public string DisplayName => CharmKind.Circle.GetDisplayName();
    public CharmMetrics Metrics => CharmMetrics.Default; // mass 2.6, radiusRatio 0.126, knotInset 0.90
    public CharmSound Sound => CharmSound.Glass;
    public IReadOnlyList<CharmBead> Beads => Array.Empty<CharmBead>();
    public CharmPalette? CordTint => null;

    public CharmPalette Palette { get; } = new(
        Primary: new CharmColor(0.49, 0.42, 0.95),
        Secondary: new CharmColor(0.33, 0.26, 0.78),
        Deep: new CharmColor(0.22, 0.16, 0.55),
        Light: new CharmColor(0.78, 0.74, 1.00)
    );

    public void Draw(DrawingContext dc, double unitSide)
    {
        double radius = unitSide * 0.5;
        var center = new Point(radius, radius);

        var fillBrush = new RadialGradientBrush(
            Palette.Light.ToMediaColor(),
            Palette.Primary.ToMediaColor())
        {
            GradientOrigin = new Point(0.35, 0.35),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };
        fillBrush.Freeze();

        var strokePen = new Pen(Palette.Deep.ToBrush(), unitSide * 0.04);
        strokePen.Freeze();

        dc.DrawEllipse(fillBrush, strokePen, center, radius * 0.94, radius * 0.94);

        // Specular glint
        var glintBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
        glintBrush.Freeze();
        dc.DrawEllipse(glintBrush, null, new Point(unitSide * 0.38, unitSide * 0.35), radius * 0.18, radius * 0.12);
    }
}

public sealed class CameraCharm : ICharm
{
    public CharmID Id => CharmID.FromBuiltIn(CharmKind.Camera);
    public string DisplayName => CharmKind.Camera.GetDisplayName();
    public CharmMetrics Metrics => new(Mass: 4.2, RadiusRatio: 0.170, KnotInset: 0.68);
    public CharmSound Sound => CharmSound.Metal;
    public IReadOnlyList<CharmBead> Beads => Array.Empty<CharmBead>();
    public CharmPalette? CordTint => null;

    public CharmPalette Palette { get; } = new(
        Primary: new CharmColor(0.36, 0.38, 0.43),
        Secondary: new CharmColor(0.20, 0.22, 0.26),
        Deep: new CharmColor(0.09, 0.10, 0.12),
        Light: new CharmColor(0.74, 0.77, 0.82)
    );

    public void Draw(DrawingContext dc, double unitSide)
    {
        var bodyBrush = Palette.Primary.ToBrush();
        var deepBrush = Palette.Deep.ToBrush();
        var secBrush = Palette.Secondary.ToBrush();
        var strokePen = new Pen(deepBrush, unitSide * 0.03);
        strokePen.Freeze();

        // Body
        var bodyRect = new Rect(unitSide * 0.06, unitSide * 0.30, unitSide * 0.88, unitSide * 0.56);
        dc.DrawRoundedRectangle(bodyBrush, strokePen, bodyRect, unitSide * 0.10, unitSide * 0.10);

        // Viewfinder bump
        var bumpRect = new Rect(unitSide * 0.28, unitSide * 0.18, unitSide * 0.26, unitSide * 0.14);
        dc.DrawRoundedRectangle(bodyBrush, strokePen, bumpRect, unitSide * 0.04, unitSide * 0.04);

        // Lens concentric rings
        var lensCenter = new Point(unitSide * 0.50, unitSide * 0.58);
        dc.DrawEllipse(deepBrush, null, lensCenter, unitSide * 0.20, unitSide * 0.20);
        dc.DrawEllipse(secBrush, null, lensCenter, unitSide * 0.15, unitSide * 0.15);
        dc.DrawEllipse(deepBrush, null, lensCenter, unitSide * 0.08, unitSide * 0.08);

        // Glint
        var glintBrush = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255));
        glintBrush.Freeze();
        dc.DrawEllipse(glintBrush, null, new Point(unitSide * 0.46, unitSide * 0.54), unitSide * 0.035, unitSide * 0.025);
    }
}

public sealed class StarCharm : ICharm
{
    public CharmID Id => CharmID.FromBuiltIn(CharmKind.Star);
    public string DisplayName => CharmKind.Star.GetDisplayName();
    public CharmMetrics Metrics => new(Mass: 2.2, RadiusRatio: 0.157, KnotInset: 0.90);
    public CharmSound Sound => CharmSound.Soft;
    public IReadOnlyList<CharmBead> Beads => Array.Empty<CharmBead>();
    public CharmPalette? CordTint => null;

    public CharmPalette Palette { get; } = new(
        Primary: new CharmColor(1.00, 0.78, 0.25),
        Secondary: new CharmColor(0.93, 0.58, 0.10),
        Deep: new CharmColor(0.62, 0.35, 0.03),
        Light: new CharmColor(1.00, 0.93, 0.70)
    );

    public void Draw(DrawingContext dc, double unitSide)
    {
        var center = new Point(unitSide * 0.50, unitSide * 0.52);
        double outerR = unitSide * 0.46;
        double innerR = unitSide * 0.20;

        var starGeo = new StreamGeometry();
        using (var ctx = starGeo.Open())
        {
            for (int i = 0; i < 10; i++)
            {
                double angle = (i * Math.PI / 5.0) - (Math.PI / 2.0);
                double r = (i % 2 == 0) ? outerR : innerR;
                var pt = new Point(center.X + (Math.Cos(angle) * r), center.Y + (Math.Sin(angle) * r));
                if (i == 0) ctx.BeginFigure(pt, isFilled: true, isClosed: true);
                else ctx.LineTo(pt, isStroked: true, isSmoothJoin: false);
            }
        }
        starGeo.Freeze();

        var fillBrush = new RadialGradientBrush(Palette.Light.ToMediaColor(), Palette.Primary.ToMediaColor())
        {
            GradientOrigin = new Point(0.4, 0.35),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };
        fillBrush.Freeze();
        var pen = new Pen(Palette.Deep.ToBrush(), unitSide * 0.035);
        pen.Freeze();

        dc.DrawGeometry(fillBrush, pen, starGeo);
    }
}

public sealed class HeartCharm : ICharm
{
    public CharmID Id => CharmID.FromBuiltIn(CharmKind.Heart);
    public string DisplayName => CharmKind.Heart.GetDisplayName();
    public CharmMetrics Metrics => new(Mass: 2.9, RadiusRatio: 0.149, KnotInset: 0.68);
    public CharmSound Sound => CharmSound.Soft;
    public IReadOnlyList<CharmBead> Beads => Array.Empty<CharmBead>();
    public CharmPalette? CordTint => null;

    public CharmPalette Palette { get; } = new(
        Primary: new CharmColor(0.95, 0.27, 0.35),
        Secondary: new CharmColor(0.78, 0.13, 0.25),
        Deep: new CharmColor(0.50, 0.06, 0.14),
        Light: new CharmColor(1.00, 0.72, 0.75)
    );

    public void Draw(DrawingContext dc, double unitSide)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(unitSide * 0.50, unitSide * 0.85), isFilled: true, isClosed: true);
            // Left curve
            ctx.BezierTo(
                new Point(unitSide * 0.10, unitSide * 0.60),
                new Point(unitSide * 0.04, unitSide * 0.28),
                new Point(unitSide * 0.28, unitSide * 0.20),
                true, false
            );
            ctx.BezierTo(
                new Point(unitSide * 0.40, unitSide * 0.20),
                new Point(unitSide * 0.48, unitSide * 0.28),
                new Point(unitSide * 0.50, unitSide * 0.34),
                true, false
            );
            // Right curve
            ctx.BezierTo(
                new Point(unitSide * 0.52, unitSide * 0.28),
                new Point(unitSide * 0.60, unitSide * 0.20),
                new Point(unitSide * 0.72, unitSide * 0.20),
                true, false
            );
            ctx.BezierTo(
                new Point(unitSide * 0.96, unitSide * 0.28),
                new Point(unitSide * 0.90, unitSide * 0.60),
                new Point(unitSide * 0.50, unitSide * 0.85),
                true, false
            );
        }
        geo.Freeze();

        var fillBrush = new RadialGradientBrush(Palette.Light.ToMediaColor(), Palette.Primary.ToMediaColor())
        {
            GradientOrigin = new Point(0.35, 0.3),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.55,
            RadiusY = 0.55
        };
        fillBrush.Freeze();
        var pen = new Pen(Palette.Deep.ToBrush(), unitSide * 0.035);
        pen.Freeze();

        dc.DrawGeometry(fillBrush, pen, geo);
    }
}

public sealed class DiamondCharm : ICharm
{
    public CharmID Id => CharmID.FromBuiltIn(CharmKind.Diamond);
    public string DisplayName => CharmKind.Diamond.GetDisplayName();
    public CharmMetrics Metrics => new(Mass: 3.4, RadiusRatio: 0.142, KnotInset: 0.80);
    public CharmSound Sound => CharmSound.Glass;
    public IReadOnlyList<CharmBead> Beads => Array.Empty<CharmBead>();
    public CharmPalette? CordTint => null;

    public CharmPalette Palette { get; } = new(
        Primary: new CharmColor(0.62, 0.88, 0.97),
        Secondary: new CharmColor(0.35, 0.68, 0.88),
        Deep: new CharmColor(0.18, 0.42, 0.62),
        Light: new CharmColor(0.90, 0.98, 1.00)
    );

    public void Draw(DrawingContext dc, double unitSide)
    {
        var topL = new Point(unitSide * 0.30, unitSide * 0.16);
        var topR = new Point(unitSide * 0.70, unitSide * 0.16);
        var midL = new Point(unitSide * 0.10, unitSide * 0.40);
        var midR = new Point(unitSide * 0.90, unitSide * 0.40);
        var bot = new Point(unitSide * 0.50, unitSide * 0.88);

        var outerGeo = new StreamGeometry();
        using (var ctx = outerGeo.Open())
        {
            ctx.BeginFigure(topL, isFilled: true, isClosed: true);
            ctx.LineTo(topR, true, false);
            ctx.LineTo(midR, true, false);
            ctx.LineTo(bot, true, false);
            ctx.LineTo(midL, true, false);
        }
        outerGeo.Freeze();

        var fillBrush = new LinearGradientBrush(Palette.Light.ToMediaColor(), Palette.Primary.ToMediaColor(), 45);
        fillBrush.Freeze();
        var pen = new Pen(Palette.Deep.ToBrush(), unitSide * 0.03);
        pen.Freeze();

        dc.DrawGeometry(fillBrush, pen, outerGeo);

        // Facets
        var facetPen = new Pen(Palette.Light.ToBrush(), unitSide * 0.02);
        facetPen.Freeze();
        dc.DrawLine(facetPen, midL, midR);
        dc.DrawLine(facetPen, topL, new Point(unitSide * 0.35, unitSide * 0.40));
        dc.DrawLine(facetPen, topR, new Point(unitSide * 0.65, unitSide * 0.40));
        dc.DrawLine(facetPen, new Point(unitSide * 0.35, unitSide * 0.40), bot);
        dc.DrawLine(facetPen, new Point(unitSide * 0.65, unitSide * 0.40), bot);
    }
}
