using System;
using System.Windows.Media;

namespace Hangly.Windows.Models;

/// <summary>
/// An sRGB color struct matching Swift's CharmColor.
/// </summary>
public readonly record struct CharmColor(double Red, double Green, double Blue, double Alpha = 1.0)
{
    public CharmColor WithAlpha(double alpha) => new(Red, Green, Blue, Math.Clamp(alpha, 0.0, 1.0));

    public CharmColor Scaled(double factor) => new(
        Math.Clamp(Red * factor, 0.0, 1.0),
        Math.Clamp(Green * factor, 0.0, 1.0),
        Math.Clamp(Blue * factor, 0.0, 1.0),
        Alpha
    );

    public static CharmColor Interpolate(CharmColor start, CharmColor end, double progress)
    {
        double amount = Math.Clamp(progress, 0.0, 1.0);
        return new CharmColor(
            start.Red + ((end.Red - start.Red) * amount),
            start.Green + ((end.Green - start.Green) * amount),
            start.Blue + ((end.Blue - start.Blue) * amount),
            start.Alpha + ((end.Alpha - start.Alpha) * amount)
        );
    }

    public Color ToMediaColor() => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(Alpha * 255.0), 0, 255),
        (byte)Math.Clamp((int)Math.Round(Red * 255.0), 0, 255),
        (byte)Math.Clamp((int)Math.Round(Green * 255.0), 0, 255),
        (byte)Math.Clamp((int)Math.Round(Blue * 255.0), 0, 255)
    );

    public SolidColorBrush ToBrush()
    {
        var brush = new SolidColorBrush(ToMediaColor());
        brush.Freeze();
        return brush;
    }
}

public enum CharmInk
{
    Primary,
    Secondary,
    Deep,
    Light,
    Glint
}

/// <summary>
/// The four tones a charm is built from (plus glint).
/// </summary>
public readonly record struct CharmPalette(
    CharmColor Primary,
    CharmColor Secondary,
    CharmColor Deep,
    CharmColor Light)
{
    public static CharmPalette Derived(CharmColor @base) => new(
        Primary: @base,
        Secondary: @base.Scaled(0.72),
        Deep: @base.Scaled(0.45),
        Light: CharmColor.Interpolate(@base, new CharmColor(1, 1, 1), 0.55)
    );

    public static CharmPalette Interpolate(CharmPalette start, CharmPalette end, double progress) => new(
        Primary: CharmColor.Interpolate(start.Primary, end.Primary, progress),
        Secondary: CharmColor.Interpolate(start.Secondary, end.Secondary, progress),
        Deep: CharmColor.Interpolate(start.Deep, end.Deep, progress),
        Light: CharmColor.Interpolate(start.Light, end.Light, progress)
    );

    public CharmColor GetColor(CharmInk ink) => ink switch
    {
        CharmInk.Primary => Primary,
        CharmInk.Secondary => Secondary,
        CharmInk.Deep => Deep,
        CharmInk.Light => Light,
        CharmInk.Glint => new CharmColor(1, 1, 1, 0.85),
        _ => Primary
    };
}
