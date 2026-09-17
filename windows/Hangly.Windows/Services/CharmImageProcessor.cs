using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hangly.Windows.Models;
using Hangly.Windows.Physics;
using Hangly.Windows.Utilities;

namespace Hangly.Windows.Services;

public enum BackgroundRemovalStrategy
{
    Automatic,
    FloodFill,
    None
}

public sealed record ProcessedCharmImage(
    byte[] PngData,
    int PixelSide,
    CharmMetrics Metrics,
    CharmPalette Palette,
    CharmSound SuggestedSound = CharmSound.Wood
);

public static class CharmImageProcessor
{
    public const int OutputSide = 512;
    public const int MaximumSourceSide = 2048;
    public const double SubjectFill = 0.92;
    public const int DefaultFloodTolerance = 36;
    public const double MeaningfulAlphaFraction = 0.02;

    public static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif"];

    public static bool IsSupported(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return Array.Exists(SupportedExtensions, e => e == ext);
    }

    public static ProcessedCharmImage Process(
        string filePath,
        BackgroundRemovalStrategy strategy = BackgroundRemovalStrategy.Automatic,
        int floodTolerance = DefaultFloodTolerance)
    {
        using var stream = File.OpenRead(filePath);
        return Process(stream, strategy, floodTolerance);
    }

    public static ProcessedCharmImage Process(
        Stream stream,
        BackgroundRemovalStrategy strategy = BackgroundRemovalStrategy.Automatic,
        int floodTolerance = DefaultFloodTolerance)
    {
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile,
            BitmapCacheOption.OnLoad
        );

        if (decoder.Frames.Count == 0)
            throw new InvalidOperationException("Could not read image frames from stream.");

        BitmapSource frame = decoder.Frames[0];

        // Downsample if needed
        int maxSide = Math.Max(frame.PixelWidth, frame.PixelHeight);
        if (maxSide > MaximumSourceSide)
        {
            double scale = (double)MaximumSourceSide / maxSide;
            int newW = Math.Max(1, (int)Math.Round(frame.PixelWidth * scale));
            int newH = Math.Max(1, (int)Math.Round(frame.PixelHeight * scale));

            var tb = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
            tb.Freeze();
            frame = tb;
        }

        var bitmap = RGBABitmap.FromBitmapSource(frame)
            ?? throw new InvalidOperationException("Failed to decode bitmap into RGBA memory buffer.");

        // Background removal
        switch (strategy)
        {
            case BackgroundRemovalStrategy.None:
                break;

            case BackgroundRemovalStrategy.FloodFill:
                bitmap.FloodFillBackground(floodTolerance);
                bitmap.FeatherAlphaEdges();
                break;

            case BackgroundRemovalStrategy.Automatic:
            default:
                if (bitmap.TransparentFraction(threshold: 8) <= MeaningfulAlphaFraction)
                {
                    bitmap.FloodFillBackground(floodTolerance);
                    bitmap.FeatherAlphaEdges();
                }
                break;
        }

        // Fit to 512x512 square
        var square = bitmap.FitToSquare(OutputSide, SubjectFill);

        // Analyze mass, knot inset, palette
        var (metrics, palette) = square.Analyze();

        byte[] pngData = square.ToPngByteArray();

        return new ProcessedCharmImage(
            PngData: pngData,
            PixelSide: OutputSide,
            Metrics: metrics,
            Palette: palette,
            SuggestedSound: CharmSound.Wood
        );
    }
}
