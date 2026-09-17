using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangly.Windows.Models.Charms;

/// <summary>
/// Every built-in charm, in menu order: the Hangly collection (11), then the classics (5).
/// Total: 16 built-in charms.
/// </summary>
public static class BuiltInCharms
{
    private static readonly Dictionary<CharmKind, ICharm> _charmsByKind = new();

    static BuiltInCharms()
    {
        // 11 Collection charms
        foreach (var collectionCharm in CollectionCharmCatalog.Charms)
        {
            _charmsByKind[collectionCharm.Kind] = collectionCharm;
        }

        // 5 Classic charms
        _charmsByKind[CharmKind.Circle] = new CircleCharm();
        _charmsByKind[CharmKind.Camera] = new CameraCharm();
        _charmsByKind[CharmKind.Star] = new StarCharm();
        _charmsByKind[CharmKind.Heart] = new HeartCharm();
        _charmsByKind[CharmKind.Diamond] = new DiamondCharm();
    }

    public static IReadOnlyList<ICharm> All =>
        CollectionCharmCatalog.Charms.Cast<ICharm>().Concat(
        [
            _charmsByKind[CharmKind.Circle],
            _charmsByKind[CharmKind.Camera],
            _charmsByKind[CharmKind.Star],
            _charmsByKind[CharmKind.Heart],
            _charmsByKind[CharmKind.Diamond]
        ]).ToList();

    public static ICharm Get(CharmKind kind)
    {
        if (_charmsByKind.TryGetValue(kind, out var charm))
        {
            return charm;
        }
        return _charmsByKind[CharmKind.Circle];
    }
}
