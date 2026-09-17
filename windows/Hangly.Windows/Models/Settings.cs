using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hangly.Windows.Models;

public enum OverlayAnchor
{
    TopLeading,  // Top-Left
    Top,         // Top-Center
    TopTrailing  // Top-Right (default)
}

public sealed class OverlaySettings
{
    public bool IsEnabled { get; set; } = true;
    public OverlayAnchor Anchor { get; set; } = OverlayAnchor.TopTrailing;
    public double Scale { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;
    public double HorizontalOffset { get; set; } = 12.0;
    public double VerticalOffset { get; set; } = -12.0;
    public bool IsClickThrough { get; set; } = true;
    public string CharmId { get; set; } = "daruma";

    [JsonIgnore]
    public CharmID Charm
    {
        get => CharmID.FromStorageValue(CharmId);
        set => CharmId = value.StorageValue;
    }
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public OverlaySettings Overlay { get; set; } = new();
    public bool LaunchAtLogin { get; set; } = true;
    public List<string> FavoriteCharms { get; set; } = [];
    public bool SoundEffectsEnabled { get; set; } = true;
    public double SoundVolume { get; set; } = 0.14;
}
