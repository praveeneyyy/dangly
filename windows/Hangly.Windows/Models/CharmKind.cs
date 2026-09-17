using System;

namespace Hangly.Windows.Models;

/// <summary>
/// Identity of a built-in charm.
/// Exactly matches the 16 charms shipped with macOS Hangly (5 classics + 11 collection).
/// </summary>
public enum CharmKind
{
    // The classics
    Circle,
    Camera,
    Star,
    Heart,
    Diamond,

    // The Hangly collection
    Nazar,
    Hamsa,
    NimbuMirchi,
    Ghanta,
    DrishtiBommai,
    PanchangJie,
    Daruma,
    ManekiNeko,
    Horseshoe,
    Scarab,
    Himmeli
}

public static class CharmKindExtensions
{
    public static string ToStorageValue(this CharmKind kind) => kind switch
    {
        CharmKind.Circle => "circle",
        CharmKind.Camera => "camera",
        CharmKind.Star => "star",
        CharmKind.Heart => "heart",
        CharmKind.Diamond => "diamond",
        CharmKind.Nazar => "nazar",
        CharmKind.Hamsa => "hamsa",
        CharmKind.NimbuMirchi => "nimbuMirchi",
        CharmKind.Ghanta => "ghanta",
        CharmKind.DrishtiBommai => "drishtiBommai",
        CharmKind.PanchangJie => "panchangJie",
        CharmKind.Daruma => "daruma",
        CharmKind.ManekiNeko => "manekiNeko",
        CharmKind.Horseshoe => "horseshoe",
        CharmKind.Scarab => "scarab",
        CharmKind.Himmeli => "himmeli",
        _ => "circle"
    };

    public static CharmKind FromStorageValue(string raw) => raw switch
    {
        "circle" => CharmKind.Circle,
        "camera" => CharmKind.Camera,
        "star" => CharmKind.Star,
        "heart" => CharmKind.Heart,
        "diamond" => CharmKind.Diamond,
        "nazar" => CharmKind.Nazar,
        "hamsa" => CharmKind.Hamsa,
        "nimbuMirchi" => CharmKind.NimbuMirchi,
        "ghanta" => CharmKind.Ghanta,
        "drishtiBommai" => CharmKind.DrishtiBommai,
        "panchangJie" => CharmKind.PanchangJie,
        "daruma" => CharmKind.Daruma,
        "manekiNeko" => CharmKind.ManekiNeko,
        "horseshoe" => CharmKind.Horseshoe,
        "scarab" => CharmKind.Scarab,
        "himmeli" => CharmKind.Himmeli,
        _ => CharmKind.Circle
    };

    public static string GetDisplayName(this CharmKind kind) => kind switch
    {
        CharmKind.Circle => "Bead",
        CharmKind.Camera => "Camera",
        CharmKind.Star => "Star",
        CharmKind.Heart => "Heart",
        CharmKind.Diamond => "Diamond",
        CharmKind.Nazar => "Nazar boncuğu",
        CharmKind.Hamsa => "Hamsa",
        CharmKind.NimbuMirchi => "Nimbu-mirchi",
        CharmKind.Ghanta => "Ghanta",
        CharmKind.DrishtiBommai => "Drishti bommai",
        CharmKind.PanchangJie => "Pánchángjié",
        CharmKind.Daruma => "Daruma",
        CharmKind.ManekiNeko => "Maneki-neko",
        CharmKind.Horseshoe => "Horseshoe",
        CharmKind.Scarab => "Scarab",
        CharmKind.Himmeli => "Himmeli",
        _ => "Bead"
    };

    public static bool IsClassic(this CharmKind kind) => (int)kind <= (int)CharmKind.Diamond;
    public static bool IsCollection(this CharmKind kind) => !kind.IsClassic();
}
