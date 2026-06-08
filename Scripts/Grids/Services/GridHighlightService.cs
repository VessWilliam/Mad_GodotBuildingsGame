
using System.Collections.Generic;
using Game.Grids.Contexts;
using Games.Grids.Services.IServices;
using Godot;

namespace Game.Grids.Services;


public class GridHighlightService : IGridHighlight
{
    private static readonly Vector2I DefaultAtlas = Vector2I.Zero;
    private static readonly Vector2I DangerAtlas = new(2, 0);
    private static readonly Vector2I ExpandedAtlas = new(1, 0);

    private readonly GridHighlightContext _context;

    public GridHighlightService(GridHighlightContext context) => _context = context;

    public void HighlightTiles(IEnumerable<Vector2I> tiles, Vector2I atlasCoords)
    {
        foreach (var tile in tiles)
        {
            _context.HighlightLayer.SetCell(tile, 0, atlasCoords);
        }
    }

    public void ClearHighlightTile() => _context.HighlightLayer.Clear();

    public void HighlightBuildableTiles(bool isAttackTiles) =>
         HighlightTiles(_context.Stats.GetBuildableTileSet(isAttackTiles), DefaultAtlas);

    public void HighlightEnemyOccupiedTiles() =>
          HighlightTiles(_context.Stats.EnemyOccupiedTiles, DefaultAtlas);

    public void HighlightExpandedTiles(IEnumerable<Vector2I> expanded) =>
          HighlightTiles(expanded, ExpandedAtlas);
}




