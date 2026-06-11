
using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Game.Grids.Services.IServices;
using Godot;

namespace Game.Grids.Services;


public class GridCache
{
    private readonly IGridTile _tileService;

    private readonly Dictionary<(BuildingComponent, int), HashSet<Vector2I>> _cache = new();

    public GridCache(IGridTile tileService) => _tileService = tileService;

    public HashSet<Vector2I> GetCacheRadius(BuildingComponent b, Rect2I area, int radius)
    {
        var key = (b, radius);

        if (_cache.TryGetValue(key, out var cached)) return cached;

        var tiles = _tileService.GetTileInRadius(area, radius, _ => true).ToHashSet();

        _cache[key] = tiles;
        return tiles;
    }

    public void ClearCache() => _cache.Clear();
}