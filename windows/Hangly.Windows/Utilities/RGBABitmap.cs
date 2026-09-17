using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hangly.Windows.Models;
using Hangly.Windows.Physics;

namespace Hangly.Windows.Utilities;

public readonly record struct PixelBounds(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Width => MaxX - MinX + 1;
    public int Height => MaxY - MinY + 1;
}

/// <summary>
/// 32-bit BGRA CPU pixel buffer for charm image processing and background removal.
/// In Windows/WPF top-down coordinates: Row 0 is the top of the image.
/// </summary>
public sealed class RGBABitmap
{
    public const int BytesPerPixel = 4; // B, G, R, A

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public RGBABitmap(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and Height must be positive.");

        Width = width;
        Height = height;
        Pixels = new byte[width * height * BytesPerPixel];
    }

    public RGBABitmap(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public static RGBABitmap? FromBitmapSource(BitmapSource source)
    {
        if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
            return null;

        BitmapSource formatted = source;
        if (source.Format != PixelFormats.Bgra32)
        {
            formatted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        }

        int width = formatted.PixelWidth;
        int height = formatted.PixelHeight;
        int stride = width * BytesPerPixel;
        byte[] pixels = new byte[height * stride];
        formatted.CopyPixels(pixels, stride, 0);

        return new RGBABitmap(width, height, pixels);
    }

    public BitmapSource ToBitmapSource()
    {
        int stride = Width * BytesPerPixel;
        var bs = BitmapSource.Create(
            Width,
            Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            Pixels,
            stride
        );
        bs.Freeze();
        return bs;
    }

    public byte[] ToPngByteArray()
    {
        var bs = ToBitmapSource();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bs));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private int Offset(int x, int y) => ((y * Width) + x) * BytesPerPixel;

    public byte Alpha(int x, int y) => Pixels[Offset(x, y) + 3];

    /// <summary>
    /// Bounds of every pixel with alpha greater than threshold.
    /// Returns null if image has no visible pixels.
    /// </summary>
    public PixelBounds? OpaqueBounds(byte threshold = 8)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        bool found = false;

        for (int y = 0; y < Height; y++)
        {
            int row = y * Width * BytesPerPixel;
            for (int x = 0; x < Width; x++)
            {
                byte a = Pixels[row + (x * BytesPerPixel) + 3];
                if (a > threshold)
                {
                    found = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!found) return null;
        return new PixelBounds(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Fraction of pixels with alpha > threshold.
    /// </summary>
    public double Coverage(byte threshold = 8)
    {
        int count = 0;
        int total = Width * Height;
        for (int i = 3; i < Pixels.Length; i += BytesPerPixel)
        {
            if (Pixels[i] > threshold) count++;
        }
        return (double)count / total;
    }

    public double TransparentFraction(byte threshold = 8) => 1.0 - Coverage(threshold);

    /// <summary>
    /// Alpha-weighted average color of visible pixels.
    /// </summary>
    public CharmColor? AverageColor(byte threshold = 8)
    {
        double blueSum = 0;
        double greenSum = 0;
        double redSum = 0;
        double totalWeight = 0;

        for (int i = 0; i < Pixels.Length; i += BytesPerPixel)
        {
            double a = Pixels[i + 3];
            if (a > threshold)
            {
                // Unpremultiply: B, G, R
                blueSum += Pixels[i] * a;
                greenSum += Pixels[i + 1] * a;
                redSum += Pixels[i + 2] * a;
                totalWeight += a;
            }
        }

        if (totalWeight <= 0) return null;

        return new CharmColor(
            redSum / totalWeight / 255.0,
            greenSum / totalWeight / 255.0,
            blueSum / totalWeight / 255.0
        );
    }

    /// <summary>
    /// Flood-fills from image corners to remove flat / uniform backgrounds.
    /// </summary>
    public void FloodFillBackground(int tolerance = 36)
    {
        if (Width <= 0 || Height <= 0) return;

        bool[] visited = new bool[Width * Height];
        var queue = new Queue<int>(Width * 2 + Height * 2);

        (int X, int Y)[] corners =
        [
            (0, 0),
            (Width - 1, 0),
            (0, Height - 1),
            (Width - 1, Height - 1)
        ];

        foreach (var (cx, cy) in corners)
        {
            int seedOffset = Offset(cx, cy);
            byte seedB = Pixels[seedOffset];
            byte seedG = Pixels[seedOffset + 1];
            byte seedR = Pixels[seedOffset + 2];
            byte seedA = Pixels[seedOffset + 3];

            // An already transparent corner is not a flat background
            if (seedA <= 250) continue;

            int start = (cy * Width) + cx;
            if (visited[start]) continue;

            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int px = index * BytesPerPixel;

                byte b = Pixels[px];
                byte g = Pixels[px + 1];
                byte r = Pixels[px + 2];
                byte a = Pixels[px + 3];

                bool matches = a > 250
                    && Math.Abs((int)b - seedB) <= tolerance
                    && Math.Abs((int)g - seedG) <= tolerance
                    && Math.Abs((int)r - seedR) <= tolerance;

                if (!matches) continue;

                // Clear pixel
                Pixels[px] = 0;
                Pixels[px + 1] = 0;
                Pixels[px + 2] = 0;
                Pixels[px + 3] = 0;

                int x = index % Width;
                int y = index / Width;

                if (x > 0 && !visited[index - 1])
                {
                    visited[index - 1] = true;
                    queue.Enqueue(index - 1);
                }
                if (x < Width - 1 && !visited[index + 1])
                {
                    visited[index + 1] = true;
                    queue.Enqueue(index + 1);
                }
                if (y > 0 && !visited[index - Width])
                {
                    visited[index - Width] = true;
                    queue.Enqueue(index - Width);
                }
                if (y < Height - 1 && !visited[index + Width])
                {
                    visited[index + Width] = true;
                    queue.Enqueue(index + Width);
                }
            }
        }
    }

    /// <summary>
    /// Softens hard alpha edges by replacing edge alpha with the neighborhood mean.
    /// </summary>
    public void FeatherAlphaEdges()
    {
        if (Width < 3 || Height < 3) return;

        byte[] original = (byte[])Pixels.Clone();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int idx = Offset(x, y);
                int alpha = original[idx + 3];
                if (alpha == 0) continue;

                int sum = 0;
                int count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                        {
                            sum += original[Offset(nx, ny) + 3];
                            count++;
                        }
                    }
                }

                int mean = sum / Math.Max(count, 1);
                if (mean < alpha)
                {
                    double ratio = (double)mean / alpha;
                    Pixels[idx] = (byte)Math.Clamp((int)Math.Round(original[idx] * ratio), 0, 255);
                    Pixels[idx + 1] = (byte)Math.Clamp((int)Math.Round(original[idx + 1] * ratio), 0, 255);
                    Pixels[idx + 2] = (byte)Math.Clamp((int)Math.Round(original[idx + 2] * ratio), 0, 255);
                    Pixels[idx + 3] = (byte)mean;
                }
            }
        }
    }

    /// <summary>
    /// Crops to visible bounds and fits into a target square (default 512x512).
    /// </summary>
    public RGBABitmap FitToSquare(int outputSide = 512, double fill = 0.92)
    {
        var bounds = OpaqueBounds();
        if (!bounds.HasValue)
        {
            return new RGBABitmap(outputSide, outputSide);
        }

        var b = bounds.Value;
        double scale = (outputSide * Math.Clamp(fill, 0.3, 1.0)) / Math.Max(b.Width, b.Height);
        int targetW = Math.Max(1, (int)Math.Round(b.Width * scale));
        int targetH = Math.Max(1, (int)Math.Round(b.Height * scale));

        int targetX = (outputSide - targetW) / 2;
        int targetY = (outputSide - targetH) / 2;

        var sourceBs = ToBitmapSource();
        var cropped = new CroppedBitmap(sourceBs, new Int32Rect(b.MinX, b.MinY, b.Width, b.Height));

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(cropped, new Rect(targetX, targetY, targetW, targetH));
        }

        var rtb = new RenderTargetBitmap(outputSide, outputSide, 96, 96, PixelFormats.Bgra32);
        rtb.Render(dv);
        rtb.Freeze();

        return FromBitmapSource(rtb) ?? new RGBABitmap(outputSide, outputSide);
    }

    /// <summary>
    /// Analyzes the final fitted square for mass, knot inset, and color palette.
    /// </summary>
    public (CharmMetrics Metrics, CharmPalette Palette) Analyze()
    {
        var bounds = OpaqueBounds() ?? new PixelBounds(0, 0, Width - 1, Height - 1);
        double coverage = Coverage();
        double mass = Math.Clamp(2.0 + (3.2 * coverage), 2.0, 4.5);

        // Top-down coordinates: minY is the top-most visible edge
        double topFromTop = (double)bounds.MinY / Height;
        double knotInset = Math.Clamp((0.5 - topFromTop) / 0.5, 0.3, 1.0);

        var baseColor = AverageColor() ?? new CharmColor(0.6, 0.6, 0.65);
        return (
            new CharmMetrics(Mass: mass, RadiusRatio: 0.12, KnotInset: knotInset),
            CharmPalette.Derived(baseColor)
        );
    }

    /// <summary>
    /// Generates a 3-pass box blur soft shadow bitmap for the artwork.
    /// </summary>
    public static RGBABitmap? GenerateShadow(RGBABitmap source, int blurRadius = 8, double opacity = 0.45)
    {
        if (blurRadius <= 0) return null;

        int padding = blurRadius * 3;
        int shadowW = source.Width + (padding * 2);
        int shadowH = source.Height + (padding * 2);

        double[] alpha = new double[shadowW * shadowH];
        for (int y = 0; y < source.Height; y++)
        {
            int row = (y + padding) * shadowW;
            for (int x = 0; x < source.Width; x++)
            {
                alpha[row + x + padding] = source.Alpha(x, y);
            }
        }

        double[] scratch = new double[shadowW * shadowH];
        for (int pass = 0; pass < 3; pass++)
        {
            BoxBlur(ref alpha, ref scratch, shadowW, shadowH, blurRadius);
        }

        byte[] shadowPixels = new byte[shadowW * shadowH * BytesPerPixel];
        for (int i = 0; i < alpha.Length; i++)
        {
            byte val = (byte)Math.Clamp((int)Math.Round(alpha[i] * opacity), 0, 255);
            // Black shadow: R=0, G=0, B=0, A=val
            int px = i * BytesPerPixel;
            shadowPixels[px] = 0;
            shadowPixels[px + 1] = 0;
            shadowPixels[px + 2] = 0;
            shadowPixels[px + 3] = val;
        }

        return new RGBABitmap(shadowW, shadowH, shadowPixels);
    }

    private static void BoxBlur(ref double[] values, ref double[] scratch, int width, int height, int radius)
    {
        double span = (radius * 2) + 1;

        // Horizontal pass
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            double total = 0;
            for (int x = -radius; x <= radius; x++)
            {
                total += values[row + Math.Clamp(x, 0, width - 1)];
            }
            for (int x = 0; x < width; x++)
            {
                scratch[row + x] = total / span;
                double leaving = values[row + Math.Clamp(x - radius, 0, width - 1)];
                double entering = values[row + Math.Clamp(x + radius + 1, 0, width - 1)];
                total += entering - leaving;
            }
        }

        // Vertical pass
        for (int x = 0; x < width; x++)
        {
            double total = 0;
            for (int y = -radius; y <= radius; y++)
            {
                total += scratch[(Math.Clamp(y, 0, height - 1) * width) + x];
            }
            for (int y = 0; y < height; y++)
            {
                values[(y * width) + x] = total / span;
                double leaving = scratch[(Math.Clamp(y - radius, 0, height - 1) * width) + x];
                double entering = scratch[(Math.Clamp(y + radius + 1, 0, height - 1) * width) + x];
                total += entering - leaving;
            }
        }
    }
}
