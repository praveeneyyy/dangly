using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hangly.Windows.Models.Charms;

public static class CollectionCharmCatalog
{
    public const double FallbackKnotInset = 0.96;
    public const double BeadDensity = 6.0;
    public const double MinimumBeadMass = 0.05;

    public static readonly CharmPalette CordTint = new(
        Primary: new CharmColor(0.47, 0.34, 0.11),
        Secondary: new CharmColor(0.31, 0.22, 0.06),
        Deep: new CharmColor(0.16, 0.11, 0.03),
        Light: new CharmColor(0.78, 0.62, 0.30)
    );

    public static string? ResolveAssetPath(string fileName)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidatePaths =
        [
            Path.Combine(baseDir, "Assets", "Charms", fileName),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "Assets", "Charms", fileName),
            Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Charms", fileName),
            Path.Combine("c:\\Projects\\Hangly\\Assets\\Charms", fileName),
            Path.Combine("Assets", "Charms", fileName)
        ];

        foreach (var p in candidatePaths)
        {
            if (File.Exists(p)) return Path.GetFullPath(p);
        }
        return null;
    }

    public static readonly IReadOnlyList<CollectionCharm> Charms =
    [
        new(
            kind: CharmKind.Nazar,
            sourceFileName: "Nazar Boncuğu.svg",
            mass: 2.75,
            radiusRatio: 0.145,
            palette: new CharmPalette(
                Primary: new CharmColor(0.10, 0.22, 0.68),
                Secondary: new CharmColor(0.06, 0.12, 0.42),
                Deep: new CharmColor(0.03, 0.06, 0.25),
                Light: new CharmColor(0.42, 0.58, 0.95)
            ),
            sound: CharmSound.Glass,
            beadCount: 3
        ),
        new(
            kind: CharmKind.Hamsa,
            sourceFileName: "Hamsa.svg",
            mass: 3.05,
            radiusRatio: 0.158,
            palette: new CharmPalette(
                Primary: new CharmColor(0.15, 0.24, 0.60),
                Secondary: new CharmColor(0.09, 0.14, 0.42),
                Deep: new CharmColor(0.05, 0.08, 0.26),
                Light: new CharmColor(0.50, 0.62, 0.95)
            ),
            sound: CharmSound.Metal,
            beadCount: 3
        ),
        new(
            kind: CharmKind.NimbuMirchi,
            sourceFileName: "Nimbu-mirchi.svg",
            mass: 2.85,
            radiusRatio: 0.152,
            palette: new CharmPalette(
                Primary: new CharmColor(0.98, 0.84, 0.18),
                Secondary: new CharmColor(0.85, 0.62, 0.08),
                Deep: new CharmColor(0.55, 0.38, 0.03),
                Light: new CharmColor(1.00, 0.96, 0.62)
            ),
            sound: CharmSound.Soft,
            beadCount: 0,
            bodyRun: 0
        ),
        new(
            kind: CharmKind.Ghanta,
            sourceFileName: "Ghanta.svg",
            mass: 4.05,
            radiusRatio: 0.150,
            palette: new CharmPalette(
                Primary: new CharmColor(0.76, 0.45, 0.22),
                Secondary: new CharmColor(0.55, 0.30, 0.13),
                Deep: new CharmColor(0.32, 0.17, 0.07),
                Light: new CharmColor(0.98, 0.78, 0.55)
            ),
            sound: CharmSound.Bell,
            beadCount: 1
        ),
        new(
            kind: CharmKind.DrishtiBommai,
            sourceFileName: "Dhrishti bomma.svg",
            mass: 3.25,
            radiusRatio: 0.164,
            palette: new CharmPalette(
                Primary: new CharmColor(0.86, 0.14, 0.12),
                Secondary: new CharmColor(0.62, 0.08, 0.08),
                Deep: new CharmColor(0.36, 0.04, 0.05),
                Light: new CharmColor(1.00, 0.55, 0.45)
            ),
            sound: CharmSound.Wood,
            beadCount: 3
        ),
        new(
            kind: CharmKind.PanchangJie,
            sourceFileName: "Pánchang Jié.svg",
            mass: 2.45,
            radiusRatio: 0.160,
            palette: new CharmPalette(
                Primary: new CharmColor(0.88, 0.14, 0.18),
                Secondary: new CharmColor(0.62, 0.08, 0.10),
                Deep: new CharmColor(0.36, 0.04, 0.06),
                Light: new CharmColor(1.00, 0.62, 0.60)
            ),
            sound: CharmSound.Soft,
            beadCount: 3
        ),
        new(
            kind: CharmKind.Daruma,
            sourceFileName: "Daruma.svg",
            mass: 3.65,
            radiusRatio: 0.154,
            palette: new CharmPalette(
                Primary: new CharmColor(0.88, 0.12, 0.12),
                Secondary: new CharmColor(0.62, 0.06, 0.06),
                Deep: new CharmColor(0.38, 0.03, 0.03),
                Light: new CharmColor(1.00, 0.50, 0.40)
            ),
            sound: CharmSound.Wood,
            beadCount: 3
        ),
        new(
            kind: CharmKind.ManekiNeko,
            sourceFileName: "Maneki-neko.svg",
            mass: 3.45,
            radiusRatio: 0.167,
            palette: new CharmPalette(
                Primary: new CharmColor(0.97, 0.95, 0.90),
                Secondary: new CharmColor(0.82, 0.78, 0.70),
                Deep: new CharmColor(0.55, 0.50, 0.42),
                Light: new CharmColor(1.00, 1.00, 1.00)
            ),
            sound: CharmSound.Wood,
            beadCount: 2
        ),
        new(
            kind: CharmKind.Horseshoe,
            sourceFileName: "Horseshoe.svg",
            mass: 3.85,
            radiusRatio: 0.151,
            palette: new CharmPalette(
                Primary: new CharmColor(0.58, 0.60, 0.64),
                Secondary: new CharmColor(0.38, 0.40, 0.44),
                Deep: new CharmColor(0.20, 0.21, 0.24),
                Light: new CharmColor(0.88, 0.90, 0.93)
            ),
            sound: CharmSound.Metal,
            beadCount: 2
        ),
        new(
            kind: CharmKind.Scarab,
            sourceFileName: "Scarab.svg",
            mass: 3.15,
            radiusRatio: 0.146,
            palette: new CharmPalette(
                Primary: new CharmColor(0.20, 0.74, 0.76),
                Secondary: new CharmColor(0.10, 0.50, 0.55),
                Deep: new CharmColor(0.05, 0.30, 0.34),
                Light: new CharmColor(0.70, 0.95, 0.94)
            ),
            sound: CharmSound.Glass,
            beadCount: 3
        ),
        new(
            kind: CharmKind.Himmeli,
            sourceFileName: "Himmeli.svg",
            mass: 2.35,
            radiusRatio: 0.169,
            palette: new CharmPalette(
                Primary: new CharmColor(0.82, 0.64, 0.26),
                Secondary: new CharmColor(0.60, 0.44, 0.14),
                Deep: new CharmColor(0.36, 0.26, 0.07),
                Light: new CharmColor(0.99, 0.90, 0.60)
            ),
            sound: CharmSound.Soft,
            beadCount: 0,
            bodyRun: 2
        )
    ];

    public static CollectionCharm? Get(CharmKind kind) => Charms.FirstOrDefault(c => c.Kind == kind);
}
