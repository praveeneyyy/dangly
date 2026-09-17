using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangly.Windows.Utilities;

public readonly record struct UnitRect(double X, double Y, double Width, double Height)
{
    public double MinX => X;
    public double MaxX => X + Width;
    public double MinY => Y;
    public double MaxY => Y + Height;
    public double MidX => X + Width / 2.0;
    public double MidY => Y + Height / 2.0;
}

public sealed record CharmArtworkRegions(UnitRect Body, IReadOnlyList<UnitRect> Beads)
{
    public double KnotInset
    {
        get
        {
            double longest = Math.Max(Body.Width, Body.Height);
            return longest > 0 ? Body.Height / longest : 1.0;
        }
    }
}

/// <summary>
/// Separates a charm's body from the beads threaded above it.
/// Exact port of macOS CharmArtworkSplitter.swift.
/// </summary>
public static class CharmArtworkSplitter
{
    public const int AnalysisPixels = 320;
    public const double CordWidthFraction = 0.085;
    public const double EdgeWidthFraction = 0.25;
    public const byte AlphaThreshold = 8;

    private readonly struct RowExtent
    {
        public readonly int MinX;
        public readonly int MaxX;
        public readonly int Width;

        public RowExtent(int minX, int maxX, int width)
        {
            MinX = minX;
            MaxX = maxX;
            Width = width;
        }
    }

    private struct Run
    {
        public int First;
        public int Last;
        public int MinX;
        public int MaxX;

        public Run(int first, int last, int minX, int maxX)
        {
            First = first;
            Last = last;
            MinX = minX;
            MaxX = maxX;
        }
    }

    /// <summary>
    /// Analyzes a 320x320 32-bit BGRA or RGBA pixel buffer to find beads and body runs.
    /// </summary>
    public static CharmArtworkRegions? Split(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int alphaOffset, // 3 for BGRA/RGBA
        UnitRect contentRect,
        int beadCount,
        int? bodyRun = null)
    {
        int bodyIndex = bodyRun ?? beadCount;
        if (beadCount < 0 || bodyIndex < beadCount) return null;

        double side = AnalysisPixels;
        var rows = RowProfile(pixels, width, height, stride, alphaOffset);
        double cordWidth = CordWidthFraction * contentRect.Width * side;
        var runs = SolidRuns(rows, cordWidth);
        if (runs.Count <= bodyIndex) return null;

        var beads = new List<UnitRect>(beadCount);
        for (int i = 0; i < Math.Min(beadCount, runs.Count); i++)
        {
            var trimmed = Trim(runs[i], rows);
            beads.Add(ToUnitRect(trimmed, side));
        }

        int bodyTop = runs[bodyIndex].First;
        int bodyBottom = -1;
        for (int i = rows.Count - 1; i >= bodyTop; i--)
        {
            if (rows[i].Width > 0)
            {
                bodyBottom = i;
                break;
            }
        }
        if (bodyBottom < bodyTop) return null;

        int minX = int.MaxValue;
        int maxX = -1;
        for (int r = bodyTop; r <= bodyBottom; r++)
        {
            if (rows[r].Width > 0)
            {
                minX = Math.Min(minX, rows[r].MinX);
                maxX = Math.Max(maxX, rows[r].MaxX);
            }
        }
        if (maxX < minX) return null;

        var bodyRunStruct = new Run(bodyTop, bodyBottom, minX, maxX);
        var body = ToUnitRect(bodyRunStruct, side);

        return new CharmArtworkRegions(body, beads);
    }

    private static List<RowExtent> RowProfile(byte[] pixels, int width, int height, int stride, int alphaOffset)
    {
        var result = new List<RowExtent>(height);
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            int first = -1;
            int last = -1;

            for (int x = 0; x < width; x++)
            {
                int pixelIdx = rowStart + (x * 4) + alphaOffset;
                if (pixels[pixelIdx] > AlphaThreshold)
                {
                    if (first < 0) first = x;
                    last = x;
                }
            }

            if (first >= 0)
            {
                result.Add(new RowExtent(first, last, last - first + 1));
            }
            else
            {
                result.Add(new RowExtent(0, 0, 0));
            }
        }
        return result;
    }

    private static List<Run> SolidRuns(List<RowExtent> rows, double minimumWidth)
    {
        var runs = new List<Run>();
        Run? current = null;

        for (int index = 0; index < rows.Count; index++)
        {
            if (rows[index].Width > minimumWidth)
            {
                if (current.HasValue)
                {
                    var r = current.Value;
                    r.Last = index;
                    r.MinX = Math.Min(r.MinX, rows[index].MinX);
                    r.MaxX = Math.Max(r.MaxX, rows[index].MaxX);
                    current = r;
                }
                else
                {
                    current = new Run(index, index, rows[index].MinX, rows[index].MaxX);
                }
            }
            else if (current.HasValue)
            {
                runs.Add(current.Value);
                current = null;
            }
        }

        if (current.HasValue)
        {
            runs.Add(current.Value);
        }

        return runs;
    }

    private static Run Trim(Run run, List<RowExtent> rows)
    {
        int widest = 0;
        for (int r = run.First; r <= run.Last; r++)
        {
            widest = Math.Max(widest, rows[r].Width);
        }
        double floor = widest * EdgeWidthFraction;

        int first = run.First;
        int last = run.Last;
        while (first < last && rows[first].Width < floor) first++;
        while (last > first && rows[last].Width < floor) last--;

        int minX = int.MaxValue;
        int maxX = -1;
        for (int r = first; r <= last; r++)
        {
            if (rows[r].Width > 0)
            {
                minX = Math.Min(minX, rows[r].MinX);
                maxX = Math.Max(maxX, rows[r].MaxX);
            }
        }

        if (maxX < minX) return run;
        return new Run(first, last, minX, maxX);
    }

    private static UnitRect ToUnitRect(Run run, double side)
    {
        return new UnitRect(
            X: run.MinX / side,
            Y: run.First / side,
            Width: (run.MaxX - run.MinX + 1) / side,
            Height: (run.Last - run.First + 1) / side
        );
    }
}
