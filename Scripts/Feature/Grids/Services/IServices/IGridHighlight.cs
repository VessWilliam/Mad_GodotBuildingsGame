using System.Collections.Generic;
using Godot;

namespace Games.Feature.Grids.Services.IServices;

public interface IGridHighlight
{
    public void HighlightTiles(IEnumerable<Vector2I> tiles, Vector2I atlasCoords);

    public void ClearHighlightTile();

    public void HighlightBuildableTiles(bool isAttackTiles);

    public void HighlightEnemyOccupiedTiles();

    public void HighlightExpandTiles(Rect2I tileArea, int radius);
    
    public void HighlightAttackTiles(Rect2I tileArea, int radius);

    public void HighlightResourceTiles(Rect2I tileArea, int radius);

}