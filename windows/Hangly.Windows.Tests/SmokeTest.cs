using Xunit;

namespace Hangly.Windows.Tests;

public class SmokeTest
{
    [Fact]
    public void TestEnvironmentIsWorking()
    {
        Assert.True(true);
    }

    [Fact]
    public void CheckAllCollectionCharmsRegions()
    {
        var kinds = new[]
        {
            Hangly.Windows.Models.CharmKind.Daruma,
            Hangly.Windows.Models.CharmKind.ManekiNeko,
            Hangly.Windows.Models.CharmKind.Ghanta,
            Hangly.Windows.Models.CharmKind.Hamsa,
            Hangly.Windows.Models.CharmKind.Nazar,
            Hangly.Windows.Models.CharmKind.Scarab,
            Hangly.Windows.Models.CharmKind.DrishtiBommai,
            Hangly.Windows.Models.CharmKind.PanchangJie,
            Hangly.Windows.Models.CharmKind.NimbuMirchi,
            Hangly.Windows.Models.CharmKind.Horseshoe,
            Hangly.Windows.Models.CharmKind.Himmeli
        };

        foreach (var kind in kinds)
        {
            var charm = (Hangly.Windows.Models.Charms.CollectionCharm)Hangly.Windows.Models.Charms.BuiltInCharms.Get(kind);
            string? path = Hangly.Windows.Models.Charms.CollectionCharmCatalog.ResolveAssetPath(charm.SourceFileName);
            Assert.NotNull(path);
            Assert.True(System.IO.File.Exists(path), $"Missing file for {kind}: {path}");

            var settings = new SharpVectors.Renderers.Wpf.WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = true
            };
            var converter = new SharpVectors.Converters.FileSvgReader(settings);
            var svgDrawing = converter.Read(path);
            Assert.NotNull(svgDrawing);

            int side = Hangly.Windows.Utilities.CharmArtworkSplitter.AnalysisPixels;
            var bounds = svgDrawing.Bounds;
            double longest = Math.Max(bounds.Width, bounds.Height);
            double contentW = bounds.Width / longest;
            double contentH = bounds.Height / longest;
            double offsetX = (1.0 - contentW) / 2.0 * side;
            double offsetY = (1.0 - contentH) / 2.0 * side;
            var contentRect = new Hangly.Windows.Utilities.UnitRect((1.0 - contentW) / 2.0, (1.0 - contentH) / 2.0, contentW, contentH);

            var centeredVisual = new System.Windows.Media.DrawingVisual();
            using (var dc = centeredVisual.RenderOpen())
            {
                double scale = side / longest;
                dc.PushTransform(new System.Windows.Media.TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new System.Windows.Media.ScaleTransform(scale, scale));
                dc.PushTransform(new System.Windows.Media.TranslateTransform(-bounds.X, -bounds.Y));
                dc.DrawDrawing(svgDrawing);
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }

            int stride = side * 4;
            var centeredRtb = new System.Windows.Media.Imaging.RenderTargetBitmap(side, side, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            centeredRtb.Render(centeredVisual);
            byte[] centeredPixels = new byte[stride * side];
            centeredRtb.CopyPixels(centeredPixels, stride, 0);

            var regions = Hangly.Windows.Utilities.CharmArtworkSplitter.Split(centeredPixels, side, side, stride, alphaOffset: 3, contentRect, charm.BeadCount, charm.BodyRun);
            Assert.True(regions != null, $"Split failed for {kind} (beadCount: {charm.BeadCount}, bodyRun: {charm.BodyRun})");
            Assert.Equal(charm.BeadCount, regions.Beads.Count);
        }
    }

    [Fact]
    public void TestImageProcessingFitToSquare()
    {
        // Create a 128x128 bitmap with a blue circle
        int width = 128;
        int height = 128;
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dx = x - 64;
                int dy = y - 64;
                if (dx * dx + dy * dy <= 40 * 40)
                {
                    int idx = (y * stride) + (x * 4);
                    pixels[idx + 0] = 220; // B
                    pixels[idx + 1] = 50;  // G
                    pixels[idx + 2] = 20;  // R
                    pixels[idx + 3] = 255; // A
                }
            }
        }

        var bs = System.Windows.Media.Imaging.BitmapSource.Create(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32,
            null, pixels, stride);

        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bs));
        using var ms = new System.IO.MemoryStream();
        encoder.Save(ms);
        ms.Position = 0;

        // Verify Process runs without throwing ArgumentException or Bgra32 PixelFormat error
        var processed = Hangly.Windows.Services.CharmImageProcessor.Process(ms);
        Assert.NotNull(processed);
        Assert.NotNull(processed.PngData);
        Assert.True(processed.PngData.Length > 0);
        Assert.Equal(512, processed.PixelSide);
        Assert.True(processed.Metrics.Mass >= 2.0 && processed.Metrics.Mass <= 4.5);
    }
}
