using System.Collections.Generic;
using Game.Component;
using Godot;

namespace Games.Feature.Grids.Services.IServices;

public interface IGridCache
{
    HashSet<Vector2I> GetCacheRadius(BuildingComponent b, Rect2I area, int radius);

    void ClearCache();

    void Invalidate(BuildingComponent component); 
}

