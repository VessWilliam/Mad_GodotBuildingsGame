using System.Collections.Generic;
using Game.Component;
using Godot;

public interface IGridCache
{
    HashSet<Vector2I> GetCacheRadius(BuildingComponent b, Rect2I area, int radius);
    void ClearCache();
}

