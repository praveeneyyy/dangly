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

    public CharmCustomization Customization { get; set; } = new();
    public Dictionary<string, CharmCustomization> CharmCustomizations { get; set; } = new();

    public CharmCustomization GetCustomizationFor(string charmId)
    {
        if (CharmCustomizations.TryGetValue(charmId, out var cust))
        {
            return cust;
        }
        return Customization;
    }

    public void SetCustomizationFor(string charmId, CharmCustomization cust)
    {
        CharmCustomizations[charmId] = cust;
        Customization = cust;
    }

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
