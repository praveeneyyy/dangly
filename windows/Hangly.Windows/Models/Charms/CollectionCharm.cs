using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hangly.Windows.Physics;
using Hangly.Windows.Utilities;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Hangly.Windows.Models.Charms;

/// <summary>
/// A built-in collection charm rendered from SVG artwork and split into body and beads.
/// </summary>
public sealed class CollectionCharm : ICharm
{
    public CharmKind Kind { get; }
    public string DisplayName => Kind.GetDisplayName();
    public CharmID Id => CharmID.FromBuiltIn(Kind);
    public double Mass { get; }
    public double RadiusRatio { get; }
    public CharmPalette Palette { get; }
    public CharmSound Sound { get; }
    public int BeadCount { get; }
    public int BodyRun { get; }
    public string SourceFileName { get; }

    public CharmPalette? CordTint => CollectionCharmCatalog.CordTint;

    private CharmArtworkRegions? _regions;
    private IReadOnlyList<CharmBead>? _beads;
    private DrawingGroup? _svgDrawing;
    private bool _initialized;

    public CollectionCharm(
        CharmKind kind,
        string sourceFileName,
        double mass,
        double radiusRatio,
        CharmPalette palette,
        CharmSound sound,
        int beadCount,
        int? bodyRun = null)
    {
        Kind = kind;
        SourceFileName = sourceFileName;
        Mass = mass;
        RadiusRatio = radiusRatio;
        Palette = palette;
        Sound = sound;
        BeadCount = beadCount;
        BodyRun = bodyRun ?? beadCount;
    }

    public CharmMetrics Metrics
    {
        get
        {
            EnsureInitialized();
            double knot = _regions?.KnotInset ?? CollectionCharmCatalog.FallbackKnotInset;
            return new CharmMetrics(Mass, RadiusRatio, knot);
        }
    }

    public IReadOnlyList<CharmBead> Beads
    {
        get
        {
            EnsureInitialized();
            return _beads ?? Array.Empty<CharmBead>();
        }
    }

    public CharmArtworkRegions? Regions
    {
        get
        {
            EnsureInitialized();
            return _regions;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        string? path = CollectionCharmCatalog.ResolveAssetPath(SourceFileName);
        if (path != null && File.Exists(path))
        {
            try
            {
                var settings = new WpfDrawingSettings
                {
                    IncludeRuntime = false,
                    TextAsGeometry = true
                };
                var converter = new FileSvgReader(settings);
                _svgDrawing = converter.Read(path);

                if (_svgDrawing != null)
                {
                    _regions = MeasureRegions(_svgDrawing, BeadCount, BodyRun);
                }
            }
            catch
            {
                // Fallback handled below
            }
        }

        if (_regions != null && _regions.Beads.Count == BeadCount)
        {
            _beads = CalculateBeads(_regions, Mass);
        }
        else
        {
            // Default deterministic bead geometry matching catalog specifications
            _beads = GenerateDefaultBeads(Kind, Mass, BeadCount);
        }
    }

    private static List<CharmBead> CalculateBeads(CharmArtworkRegions regions, double mass)
    {
        double longest = Math.Max(regions.Body.Width, regions.Body.Height);
        if (longest <= 0) return [];
        double scale = 2.0 / longest;

        var list = new List<CharmBead>(regions.Beads.Count);
        foreach (var rect in regions.Beads)
        {
            var size = new Vector2D(rect.Width * scale, rect.Height * scale);
            double radius = Math.Sqrt(size.X * size.Y) / 2.0;
            double offset = (regions.Body.MinY - rect.MidY) * scale;
            double beadMass = Math.Max(
                CollectionCharmCatalog.MinimumBeadMass,
                mass * Math.Pow(radius, 3) * CollectionCharmCatalog.BeadDensity
            );
            list.Add(new CharmBead(size, offset, beadMass));
        }
        return list;
    }

    private static List<CharmBead> GenerateDefaultBeads(CharmKind kind, double mass, int count)
    {
        if (count <= 0) return [];

        var list = new List<CharmBead>(count);
        // Distribute beads top-to-bottom above the knot
        for (int i = 0; i < count; i++)
        {
            // Top bead (anchor nearest) has highest offset
            double offset = 0.85 - (i * 0.28);
            var size = new Vector2D(0.22, 0.20);
            double radius = Math.Sqrt(size.X * size.Y) / 2.0;
            double beadMass = Math.Max(
                CollectionCharmCatalog.MinimumBeadMass,
                mass * Math.Pow(radius, 3) * CollectionCharmCatalog.BeadDensity
            );
            list.Add(new CharmBead(size, offset, beadMass));
        }
        return list;
    }

    private readonly Dictionary<(UnitRect Region, int Width, int Height), BitmapSource> _regionCache = new();
    private BitmapSource? _bodyShadow;

    private BitmapSource? RasterRegion(UnitRect region, int pixelWidth, int pixelHeight)
    {
        if (_svgDrawing == null || pixelWidth <= 0 || pixelHeight <= 0) return null;
        var key = (region, pixelWidth, pixelHeight);
        if (_regionCache.TryGetValue(key, out var cached)) return cached;

        try
        {
            var bounds = _svgDrawing.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            double longest = Math.Max(bounds.Width, bounds.Height);
            double contentW = bounds.Width / longest;
            double contentH = bounds.Height / longest;
            var content = new UnitRect((1.0 - contentW) / 2.0, (1.0 - contentH) / 2.0, contentW, contentH);

            double scaleX = (double)pixelWidth / region.Width;
            double scaleY = (double)pixelHeight / region.Height;
            double targetW = content.Width * scaleX;
            double targetH = content.Height * scaleY;
            double targetLeft = (content.MinX - region.MinX) * scaleX;
            double targetTop = (content.MinY - region.MinY) * scaleY;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.PushClip(new RectangleGeometry(new Rect(0, 0, pixelWidth, pixelHeight)));
                dc.PushTransform(new TranslateTransform(targetLeft, targetTop));
                dc.PushTransform(new ScaleTransform(targetW / bounds.Width, targetH / bounds.Height));
                dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
                dc.DrawDrawing(_svgDrawing);
                dc.Pop();
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }

            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            _regionCache[key] = rtb;
            return rtb;
        }
        catch
        {
            return null;
        }
    }

    private static CharmArtworkRegions? MeasureRegions(DrawingGroup drawing, int beadCount, int bodyRun)
    {
        try
        {
            int side = CharmArtworkSplitter.AnalysisPixels;
            var bounds = drawing.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            double longest = Math.Max(bounds.Width, bounds.Height);
            double contentW = bounds.Width / longest;
            double contentH = bounds.Height / longest;
            double offsetX = (1.0 - contentW) / 2.0 * side;
            double offsetY = (1.0 - contentH) / 2.0 * side;
            var contentRect = new UnitRect((1.0 - contentW) / 2.0, (1.0 - contentH) / 2.0, contentW, contentH);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                double scale = side / longest;
                dc.PushTransform(new TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
                dc.DrawDrawing(drawing);
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }

            var rtb = new RenderTargetBitmap(side, side, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            int stride = side * 4;
            byte[] pixels = new byte[stride * side];
            rtb.CopyPixels(pixels, stride, 0);

            return CharmArtworkSplitter.Split(pixels, side, side, stride, alphaOffset: 3, contentRect, beadCount, bodyRun);
        }
        catch
        {
            return null;
        }
    }

    public void DrawBody(DrawingContext dc, double unitSide)
    {
        EnsureInitialized();
        if (_regions != null && _svgDrawing != null)
        {
            var body = _regions.Body;
            double longest = Math.Max(body.Width, body.Height);
            if (longest > 0)
            {
                double width = unitSide * (body.Width / longest);
                double height = unitSide * (body.Height / longest);
                var targetRect = new Rect((unitSide - width) / 2.0, (unitSide - height) / 2.0, width, height);

                int pixelW = Math.Max(32, ((int)Math.Ceiling(width * 2) + 31) / 32 * 32);
                int pixelH = Math.Max(32, ((int)Math.Ceiling(height * 2) + 31) / 32 * 32);

                var bmp = RasterRegion(body, pixelW, pixelH);
                if (bmp != null)
                {
                    if (_bodyShadow == null)
                    {
                        var rgba = RGBABitmap.FromBitmapSource(bmp);
                        if (rgba != null)
                        {
                            var shadow = RGBABitmap.GenerateShadow(rgba, blurRadius: 8, opacity: 0.35);
                            if (shadow != null)
                            {
                                _bodyShadow = shadow.ToBitmapSource();
                                _bodyShadow.Freeze();
                            }
                        }
                    }

                    if (_bodyShadow != null)
                    {
                        double shadowPadding = 24.0 / pixelW;
                        double sW = width * (1.0 + shadowPadding * 2);
                        double sH = height * (1.0 + shadowPadding * 2);
                        var shadowRect = new Rect(
                            targetRect.X - (width * shadowPadding),
                            targetRect.Y - (height * shadowPadding) + (height * 0.05),
                            sW, sH);
                        dc.DrawImage(_bodyShadow, shadowRect);
                    }

                    dc.DrawImage(bmp, targetRect);
                    return;
                }
            }
        }

        Draw(dc, unitSide);
    }

    public bool DrawBead(DrawingContext dc, int beadIndex, double width, double height)
    {
        EnsureInitialized();
        if (_regions != null && beadIndex >= 0 && beadIndex < _regions.Beads.Count && _svgDrawing != null)
        {
            var beadRect = _regions.Beads[beadIndex];
            int pixelW = Math.Max(16, ((int)Math.Ceiling(width * 2) + 15) / 16 * 16);
            int pixelH = Math.Max(16, ((int)Math.Ceiling(height * 2) + 15) / 16 * 16);

            var bmp = RasterRegion(beadRect, pixelW, pixelH);
            if (bmp != null)
            {
                dc.DrawImage(bmp, new Rect(-width / 2.0, -height / 2.0, width, height));
                return true;
            }
        }
        return false;
    }

    public void Draw(DrawingContext dc, double unitSide)
    {
        EnsureInitialized();
        if (_svgDrawing != null)
        {
            var bounds = _svgDrawing.Bounds;
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                double longest = Math.Max(bounds.Width, bounds.Height);
                double scale = unitSide / longest;
                double offsetX = (unitSide - (bounds.Width * scale)) / 2.0;
                double offsetY = (unitSide - (bounds.Height * scale)) / 2.0;

                dc.PushTransform(new TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
                dc.DrawDrawing(_svgDrawing);
                dc.Pop();
                dc.Pop();
                dc.Pop();
                return;
            }
        }

        // Placeholder if SVG is missing
        var fallbackBrush = Palette.Primary.ToBrush();
        dc.DrawEllipse(fallbackBrush, null, new Point(unitSide * 0.5, unitSide * 0.5), unitSide * 0.45, unitSide * 0.45);
    }
}
