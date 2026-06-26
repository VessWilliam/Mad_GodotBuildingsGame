using System;
using System.Collections.Generic;
using System.Linq;
using Game.Extentions;
using Game.Feature.Grids.Services.IServices;
using Game.Utils;
using Godot;

namespace Game.Feature.Grids.Services;

public class GridTileServices : IGridTile
{
    private readonly List<TileMapLayer> _tilemapLayer;
    private readonly Dictionary<TileMapLayer, ElevationLayer> _tilemapElevationLayer;
    private readonly Dictionary<Vector2I, (TileMapLayer, bool)> _buildableCache = new();
    private readonly Dictionary<Vector2I, (TileMapLayer, bool)> _woodCache = new();

    public GridTileServices(List<TileMapLayer> tilemapLayer,
         Dictionary<TileMapLayer, ElevationLayer> tilemapElevationLayer)
    {
        _tilemapLayer = tilemapLayer;
        _tilemapElevationLayer = tilemapElevationLayer;
    }

    public bool IsTileAreaBuildable(Rect2I tileArea, HashSet<Vector2I> buildableTiles, HashSet<Vector2I> occupiedTiles, bool isAttackTiles = false)
    {
        var tiles = tileArea.ToTiles();

        if (tiles.Count is 0) return false;

        var (firstLayer, _) = GetBuildableData(tiles[0]);
        var elevationTile = firstLayer is not null ? _tilemapElevationLayer[firstLayer] : null;

        if (isAttackTiles) buildableTiles = buildableTiles.Except(occupiedTiles).ToHashSet();

        return tiles.All(tpos =>
        {
            var (tileMapLayer, isBuildable) = GetBuildableData(tpos);
            var elevationLayer = tileMapLayer is null ? null : _tilemapElevationLayer[tileMapLayer];
            return isBuildable && buildableTiles.Contains(tpos) && elevationLayer == elevationTile;
        });
    }

    public List<Vector2I> GetPlacementTilesInRadiusList(Rect2I tileArea, int radius) =>
        GetTileInRadius(tileArea, radius, (tilePosition) =>
            GetBuildableData(tilePosition).Item2);

    public List<Vector2I> GetResourceTilesInRadiusList(Rect2I tileArea, int radius) =>
        GetTileInRadius(tileArea, radius, (tilePosition) =>
            GetWoodData(tilePosition).Item2);

    public List<Vector2I> GetTileInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filter)
    {
        var result = new List<Vector2I>();
        var tileAreaF = tileArea.ToRect2F();
        var tileAreaCenter = tileAreaF.GetCenter();
        var radiusMax = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;

        for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
        {
            for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
            {
                float totalRadius = radius + radiusMax;
                var tilePosition = new Vector2I(x, y);
                bool isNotInside = !IsTileInsideCircle(tileAreaCenter, tilePosition, totalRadius);

                if (isNotInside || !filter(tilePosition)) continue;

                result.Add(tilePosition);
            }
        }
        return result;
    }

    private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
    {
        var distX = centerPosition.X - (tilePosition.X + .5);
        var distY = centerPosition.Y - (tilePosition.Y + .5);
        var distSquared = (distX * distX) + (distY * distY);
        return distSquared <= radius * radius;
    }

    private (TileMapLayer, bool) TileCustomData(Vector2I tilePosition, string data)
    {
        foreach (var item in _tilemapLayer)
        {
            TileData customData = item.GetCellTileData(tilePosition);

            if (customData is null || customData.GetCustomData(Constants.IS_IGNORE).AsBool())
                continue;

            return (item, customData.GetCustomData(data).AsBool());
        }

        return (null, false);
    }

    private (TileMapLayer, bool) GetBuildableData(Vector2I tilePosition)
    {
        if (_buildableCache.TryGetValue(tilePosition, out var value))
            return value;

        value = TileCustomData(tilePosition, Constants.IS_BUILDABLE);
        _buildableCache[tilePosition] = value;
        return value;
    }

    private (TileMapLayer, bool) GetWoodData(Vector2I tilePosition)
    {
        if (_woodCache.TryGetValue(tilePosition, out var value))
            return value;

        value = TileCustomData(tilePosition, Constants.IS_WOOD);
        _woodCache[tilePosition] = value;
        return value;
    }
}