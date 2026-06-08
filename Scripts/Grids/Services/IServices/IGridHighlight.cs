using System.Collections.Generic;
using Godot;

namespace Games.Grids.Services.IServices;

public interface IGridHighlight
{
    public void HighlightTiles(IEnumerable<Vector2I> tiles, Vector2I atlasCoords);

    public void ClearHighlightTile();

    public void HighlightBuildableTiles(bool isAttackTiles);

    public void HighlightEnemyOccupiedTiles();

    public void HighlightExpandedTiles(IEnumerable<Vector2I> expanded);
}