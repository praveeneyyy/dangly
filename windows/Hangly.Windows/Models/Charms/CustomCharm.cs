using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hangly.Windows.Physics;
using Hangly.Windows.Utilities;

namespace Hangly.Windows.Models.Charms;

public sealed class CustomCharmEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ImageFileName { get; set; } = string.Empty;
    public CharmMetrics Metrics { get; set; }
    public CharmPalette Palette { get; set; }
    public CharmSound Sound { get; set; } = CharmSound.Wood;
    public string? CustomSoundPath { get; set; }
}

public sealed class CustomCharm : ICharm
{
    public CustomCharmEntry Entry { get; }
    public BitmapSource Bitmap { get; }

    private readonly BitmapSource? _shadowBitmap;

    public CharmID Id => CharmID.FromCustom(Entry.Id);
    public string DisplayName => Entry.Name;
    public CharmMetrics Metrics => Entry.Metrics;
    public CharmPalette Palette => Entry.Palette;
    public CharmSound Sound => Entry.Sound;
    public string? CustomSoundPath => Entry.CustomSoundPath;
    public IReadOnlyList<CharmBead> Beads => [];
    public CharmPalette? CordTint => Entry.Palette;

    public CustomCharm(CustomCharmEntry entry, BitmapSource bitmap)
    {
        Entry = entry;
        Bitmap = bitmap;
        Bitmap.Freeze();

        // Precompute drop shadow
        var rgba = RGBABitmap.FromBitmapSource(bitmap);
        if (rgba != null)
        {
            var shadow = RGBABitmap.GenerateShadow(rgba, blurRadius: 10, opacity: 0.35);
            if (shadow != null)
            {
                _shadowBitmap = shadow.ToBitmapSource();
            }
        }
    }

    public void Draw(DrawingContext dc, double unitSide)
    {
        if (unitSide <= 0) return;

        // Draw shadow first if available
        if (_shadowBitmap != null)
        {
            // Shadow is padded by radius * 3 (30px out of 512 + 60 = 572)
            double paddingRatio = 30.0 / 512.0;
            double shadowSide = unitSide * (1.0 + (paddingRatio * 2.0));
            double offset = -unitSide * paddingRatio;
            dc.DrawImage(_shadowBitmap, new Rect(offset, offset + (unitSide * 0.04), shadowSide, shadowSide));
        }

        // Draw main charm artwork inside unit square
        dc.DrawImage(Bitmap, new Rect(0, 0, unitSide, unitSide));
    }
}
