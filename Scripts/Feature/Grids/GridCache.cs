using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Game.Feature.Grids.Services.IServices;
using Games.Feature.Grids.Services.IServices;
using Godot;

namespace Game.Feature.Grids.Services;

public enum RadiusType
{
    All,
    Building,
    Attack,
    Resource,
    Danger
}

public class GridCache : IGridCache
{
    private readonly IGridTile _tileService;
    private readonly Dictionary<(BuildingComponent, RadiusType, int), HashSet<Vector2I>> _cache = new();

    public GridCache(IGridTile tileService) => _tileService = tileService;

  public HashSet<Vector2I> GetCacheRadius(BuildingComponent component, Rect2I area, int radius, RadiusType radiusType)
{
    var key = (component, radiusType, radius);

    if (_cache.TryGetValue(key, out var cached)) 
    {
        GD.Print($"Cache hit for {component.BuildingResource.DisplayName} - {radiusType} with {cached.Count} tiles");
        return cached;
    }

    var tiles = radiusType switch
    {
        RadiusType.Building => _tileService.GetPlacementTilesInRadiusList(area, radius).ToHashSet(),
        RadiusType.Resource => _tileService.GetResourceTilesInRadiusList(area, radius).ToHashSet(),
        RadiusType.Danger => _tileService.GetPlacementTilesInRadiusList(area, radius).ToHashSet(),
        RadiusType.Attack => _tileService.GetTileInRadius(area, radius, _ => true).ToHashSet(),
        RadiusType.All => _tileService.GetTileInRadius(area, radius, _ => true).ToHashSet(),
        _ => _tileService.GetTileInRadius(area, radius, _ => true).ToHashSet()
    };

    GD.Print($"Cache miss for {component.BuildingResource.DisplayName} - {radiusType} - found {tiles.Count} tiles");
    _cache[key] = tiles;
    return tiles;
}

    public HashSet<Vector2I> GetCacheRadius(BuildingComponent component, Rect2I area, int radius)
    {
        return GetCacheRadius(component, area, radius, RadiusType.All);
    }

    public void ClearCache() => _cache.Clear();

    public void Invalidate(BuildingComponent component)
    {
        if (component == null) return;
        
        var keysToRemove = _cache.Keys
            .Where(k => k.Item1 == component || !GodotObject.IsInstanceValid(k.Item1))
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }
}