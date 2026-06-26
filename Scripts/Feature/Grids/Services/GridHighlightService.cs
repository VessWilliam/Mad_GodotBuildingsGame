using System;
using System.Collections.Generic;
using System.Linq;
using Game.Extentions;
using Games.Feature.Grids.Services.IServices;
using Godot;

namespace Game.Feature.Grids.Services;

public class GridHighlightService : IGridHighlight
{
    private static readonly Vector2I DefaultAtlas = Vector2I.Zero;
    private static readonly Vector2I DangerAtlas = new(2, 0);
    private static readonly Vector2I ExpandedAtlas = new(1, 0);

    private readonly TileMapLayer _highlightLayer;
    private readonly GridStats _stats;
    private readonly Func<Rect2I, int, List<Vector2I>> _getValidTiles;
    private readonly Func<Rect2I, int, List<Vector2I>> _getResourceTiles;

    public GridHighlightService(
        TileMapLayer highlightLayer,
        GridStats stats,
        Func<Rect2I, int, List<Vector2I>> getValidTiles,
        Func<Rect2I, int, List<Vector2I>> getResourceTiles)
    {
        _highlightLayer = highlightLayer;
        _stats = stats;
        _getValidTiles = getValidTiles;
        _getResourceTiles = getResourceTiles;
    }

    public void HighlightTiles(IEnumerable<Vector2I> tiles, Vector2I atlasCoords)
    {
        foreach (var tile in tiles)
            _highlightLayer.SetCell(tile, 0, atlasCoords);
    }

    public void ClearHighlightTile() => _highlightLayer.Clear();

    public void HighlightBuildableTiles(bool isAttackTiles) =>
        HighlightTiles(_stats.GetBuildableTileSet(isAttackTiles), DefaultAtlas);

    public void HighlightEnemyOccupiedTiles() =>
        HighlightTiles(_stats.EnemyOccupiedTiles, DangerAtlas);

    public void HighlightExpandTiles(Rect2I tileArea, int radius)
    {
        var validTiles = _getValidTiles(tileArea, radius).ToHashSet()
            .Except(_stats.BuildableTiles)
            .Except(_stats.OccupiedTiles);

        HighlightTiles(validTiles, ExpandedAtlas);
    }

    public void HighlightAttackTiles(Rect2I tileArea, int radius)
    {
        var buildingAreaTiles = tileArea.ToTiles();
        var validTiles = _getValidTiles(tileArea, radius).ToHashSet()
            .Except(_stats.AttackBuildableTiles)
            .Except(buildingAreaTiles);

        HighlightTiles(validTiles, ExpandedAtlas);
    }

    public void HighlightResourceTiles(Rect2I tileArea, int radius)
    {
        var resourceTiles = _getResourceTiles(tileArea, radius);
        HighlightTiles(resourceTiles, ExpandedAtlas);
    }
}