using Godot;

namespace Game.Grids.Contexts;

public class GridHighlightContext
{
    public GridHighlightContext(TileMapLayer highlightLayer) =>
        HighlightLayer = highlightLayer;

    public TileMapLayer HighlightLayer { get; set; }
}



