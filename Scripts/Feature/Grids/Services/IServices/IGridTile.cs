using System;
using System.Collections.Generic;
using Godot;

namespace Game.Feature.Grids.Services.IServices;

public interface IGridTile
{
    bool IsTileAreaBuildable(Rect2I tileArea, HashSet<Vector2I> buildableTiles, HashSet<Vector2I> occupiedTiles, bool isAttackTiles = false);
    List<Vector2I> GetPlacementTilesInRadiusList(Rect2I tileArea, int radius);
    List<Vector2I> GetResourceTilesInRadiusList(Rect2I tileArea, int radius);
    List<Vector2I> GetTileInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filter);
}
