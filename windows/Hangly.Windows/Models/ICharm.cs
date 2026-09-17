using System;
using System.Collections.Generic;
using System.Windows.Media;
using Hangly.Windows.Physics;

namespace Hangly.Windows.Models;

/// <summary>
/// Identifier of any charm, built-in or custom import.
/// </summary>
public readonly record struct CharmID : IEquatable<CharmID>
{
    public CharmKind? BuiltInKind { get; }
    public Guid? CustomId { get; }

    public bool IsCustom => CustomId.HasValue;

    public CharmID(CharmKind kind)
    {
        BuiltInKind = kind;
        CustomId = null;
    }

    public CharmID(Guid customId)
    {
        BuiltInKind = null;
        CustomId = customId;
    }

    public static CharmID FromBuiltIn(CharmKind kind) => new(kind);
    public static CharmID FromCustom(Guid id) => new(id);

    public string StorageValue => IsCustom
        ? "custom:" + CustomId!.Value.ToString("D")
        : BuiltInKind!.Value.ToStorageValue();

    public static CharmID FromStorageValue(string storage)
    {
        if (storage.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(storage["custom:".Length..], out var guid))
            {
                return new CharmID(guid);
            }
        }
        return new CharmID(CharmKindExtensions.FromStorageValue(storage));
    }

    public override string ToString() => StorageValue;
}

/// <summary>
/// Something that can hang on the end of the rope.
/// </summary>
public interface ICharm
{
    CharmID Id { get; }
    string DisplayName { get; }
    CharmMetrics Metrics { get; }
    CharmPalette Palette { get; }
    CharmSound Sound { get; }
    IReadOnlyList<CharmBead> Beads { get; }
    CharmPalette? CordTint { get; }

    /// <summary>
    /// Renders the charm inside a 1x1 unit square centered or fitted appropriately (e.g. for library cards).
    /// </summary>
    void Draw(DrawingContext dc, double unitSide);

    /// <summary>
    /// Renders only the charm body (excluding beads on the cord) hanging on the rope, centered in unitSide.
    /// </summary>
    void DrawBody(DrawingContext dc, double unitSide) => Draw(dc, unitSide);

    /// <summary>
    /// Renders a specific bead slice (0-indexed from top) for vector charms.
    /// Returns false if the bead should be rendered using the default styled gradient ellipse.
    /// </summary>
    bool DrawBead(DrawingContext dc, int beadIndex, double width, double height) => false;

    /// <summary>
    /// Optional file path to a custom WAV audio file.
    /// </summary>
    string? CustomSoundPath => null;
}
