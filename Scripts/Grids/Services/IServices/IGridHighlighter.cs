using System.Collections.Generic;
using Godot;

namespace Games.Grids.Services.IServices;

public interface IGridHighlighter
{
    public void HighlightTiles(IEnumerable<Vector2I> tiles, Vector2I atlasCoords);

    public void ClearHighlightTile();
}