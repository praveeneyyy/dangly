using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hangly.Windows.Models;

/// <summary>
/// Comprehensive customization configuration for a charm and its hanging cord.
/// Serves as the single source of truth for both the Charm Studio live preview and desktop overlay.
/// </summary>
public sealed class CharmCustomization
{
    // MARK: - Rope Properties
    public string? RopeColor { get; set; } = null; // null = use charm default palette
    public double RopeThickness { get; set; } = 1.0; // 0.5x to 3.0x
    public double RopeLength { get; set; } = 1.0; // 0.5x to 2.0x
    public double RopeOpacity { get; set; } = 1.0; // 0.1 to 1.0

    // MARK: - Charm Size & Transformations
    public double CharmScale { get; set; } = 1.0; // 0.5x to 2.5x
    public double RotationAngle { get; set; } = 0.0; // -180.0 to 180.0 degrees
    public bool FlipHorizontal { get; set; } = false;
    public bool FlipVertical { get; set; } = false;
    public double CharmOpacity { get; set; } = 1.0; // 0.1 to 1.0

    // MARK: - Charm Colors
    public string? PrimaryColor { get; set; } = null;
    public string? SecondaryColor { get; set; } = null;
    public string? AccentColor { get; set; } = null;
    public string? OutlineColor { get; set; } = null;

    // MARK: - Background Customization
    public string BackgroundColor { get; set; } = "#16161A";
    public bool BackgroundGradientEnabled { get; set; } = false;
    public string GradientStartColor { get; set; } = "#1F1F26";
    public string GradientEndColor { get; set; } = "#0E0E11";
    public double GradientAngle { get; set; } = 45.0; // 0 to 360 degrees
    public double BackgroundOpacity { get; set; } = 1.0;

    // MARK: - Charm Material / Appearance Effects
    public double ShadowIntensity { get; set; } = 0.35; // 0.0 to 1.0
    public double GlowIntensity { get; set; } = 0.20; // 0.0 to 1.0
    public double ShineIntensity { get; set; } = 0.50; // 0.0 to 1.0
    public double OutlineThickness { get; set; } = 0.0; // 0.0 to 6.0 px

    // MARK: - Preset
    public string ActivePreset { get; set; } = "Default";

    public CharmCustomization Clone()
    {
        return new CharmCustomization
        {
            RopeColor = RopeColor,
            RopeThickness = RopeThickness,
            RopeLength = RopeLength,
            RopeOpacity = RopeOpacity,
            CharmScale = CharmScale,
            RotationAngle = RotationAngle,
            FlipHorizontal = FlipHorizontal,
            FlipVertical = FlipVertical,
            CharmOpacity = CharmOpacity,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            OutlineColor = OutlineColor,
            BackgroundColor = BackgroundColor,
            BackgroundGradientEnabled = BackgroundGradientEnabled,
            GradientStartColor = GradientStartColor,
            GradientEndColor = GradientEndColor,
            GradientAngle = GradientAngle,
            BackgroundOpacity = BackgroundOpacity,
            ShadowIntensity = ShadowIntensity,
            GlowIntensity = GlowIntensity,
            ShineIntensity = ShineIntensity,
            OutlineThickness = OutlineThickness,
            ActivePreset = ActivePreset
        };
    }

    public void ResetRope()
    {
        RopeColor = null;
        RopeThickness = 1.0;
        RopeLength = 1.0;
        RopeOpacity = 1.0;
        ActivePreset = "Custom";
    }

    public void ResetCharm()
    {
        CharmScale = 1.0;
        RotationAngle = 0.0;
        FlipHorizontal = false;
        FlipVertical = false;
        CharmOpacity = 1.0;
        PrimaryColor = null;
        SecondaryColor = null;
        AccentColor = null;
        OutlineColor = null;
        ActivePreset = "Custom";
    }

    public void ResetAppearance()
    {
        ShadowIntensity = 0.35;
        GlowIntensity = 0.20;
        ShineIntensity = 0.50;
        OutlineThickness = 0.0;
        BackgroundColor = "#16161A";
        BackgroundGradientEnabled = false;
        GradientStartColor = "#1F1F26";
        GradientEndColor = "#0E0E11";
        GradientAngle = 45.0;
        BackgroundOpacity = 1.0;
        ActivePreset = "Custom";
    }

    public void ResetAll()
    {
        ResetRope();
        ResetCharm();
        ResetAppearance();
        ActivePreset = "Default";
    }

    public void ApplyPreset(string presetName)
    {
        ActivePreset = presetName;
        switch (presetName.ToLowerInvariant())
        {
            case "minimal":
                RopeColor = "#52525B";
                RopeThickness = 0.85;
                RopeLength = 0.95;
                RopeOpacity = 0.90;
                CharmScale = 0.95;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                ShadowIntensity = 0.15;
                GlowIntensity = 0.05;
                ShineIntensity = 0.20;
                OutlineThickness = 0.0;
                BackgroundColor = "#0F0F12";
                BackgroundGradientEnabled = false;
                break;

            case "neon":
                RopeColor = "#A855F7";
                RopeThickness = 1.25;
                RopeLength = 1.05;
                RopeOpacity = 1.0;
                CharmScale = 1.10;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                PrimaryColor = "#C084FC";
                SecondaryColor = "#7E22CE";
                AccentColor = "#22D3EE";
                ShadowIntensity = 0.60;
                GlowIntensity = 0.85;
                ShineIntensity = 0.90;
                OutlineThickness = 1.5;
                OutlineColor = "#38BDF8";
                BackgroundColor = "#070709";
                BackgroundGradientEnabled = true;
                GradientStartColor = "#1E1035";
                GradientEndColor = "#0A0512";
                GradientAngle = 135.0;
                break;

            case "ocean":
                RopeColor = "#0284C7";
                RopeThickness = 1.15;
                RopeLength = 1.0;
                RopeOpacity = 1.0;
                CharmScale = 1.0;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                PrimaryColor = "#38BDF8";
                SecondaryColor = "#0369A1";
                AccentColor = "#2DD4BF";
                ShadowIntensity = 0.40;
                GlowIntensity = 0.45;
                ShineIntensity = 0.65;
                OutlineThickness = 1.0;
                OutlineColor = "#0EA5E9";
                BackgroundColor = "#08101C";
                BackgroundGradientEnabled = true;
                GradientStartColor = "#0C2340";
                GradientEndColor = "#050B14";
                GradientAngle = 90.0;
                break;

            case "sunset":
                RopeColor = "#F97316";
                RopeThickness = 1.2;
                RopeLength = 1.0;
                RopeOpacity = 1.0;
                CharmScale = 1.05;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                PrimaryColor = "#FB923C";
                SecondaryColor = "#BE123C";
                AccentColor = "#FDE047";
                ShadowIntensity = 0.45;
                GlowIntensity = 0.50;
                ShineIntensity = 0.75;
                OutlineThickness = 1.0;
                OutlineColor = "#EA580C";
                BackgroundColor = "#180B0F";
                BackgroundGradientEnabled = true;
                GradientStartColor = "#2A0E18";
                GradientEndColor = "#0F0508";
                GradientAngle = 45.0;
                break;

            case "monochrome":
                RopeColor = "#71717A";
                RopeThickness = 1.0;
                RopeLength = 1.0;
                RopeOpacity = 1.0;
                CharmScale = 1.0;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                PrimaryColor = "#A1A1AA";
                SecondaryColor = "#3F3F46";
                AccentColor = "#E4E4E7";
                OutlineColor = "#27272A";
                ShadowIntensity = 0.25;
                GlowIntensity = 0.10;
                ShineIntensity = 0.30;
                OutlineThickness = 1.2;
                BackgroundColor = "#121215";
                BackgroundGradientEnabled = false;
                break;

            case "candy":
                RopeColor = "#EC4899";
                RopeThickness = 1.25;
                RopeLength = 1.05;
                RopeOpacity = 1.0;
                CharmScale = 1.15;
                RotationAngle = 0.0;
                FlipHorizontal = false;
                FlipVertical = false;
                CharmOpacity = 1.0;
                PrimaryColor = "#F472B6";
                SecondaryColor = "#DB2777";
                AccentColor = "#34D399";
                OutlineColor = "#BE185D";
                ShadowIntensity = 0.40;
                GlowIntensity = 0.40;
                ShineIntensity = 0.80;
                OutlineThickness = 1.5;
                BackgroundColor = "#1A0C16";
                BackgroundGradientEnabled = true;
                GradientStartColor = "#2E1026";
                GradientEndColor = "#11060F";
                GradientAngle = 60.0;
                break;

            default:
                // Default
                ResetAll();
                ActivePreset = "Default";
                break;
        }
    }
}
