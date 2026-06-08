using Godot;

namespace Game.Grids.Contexts;

public class GridHighlightContext
{
    public GridHighlightContext(TileMapLayer highlightLayer, GridStats stats)
    {
        HighlightLayer = highlightLayer;
        Stats = stats;

    }

    public TileMapLayer HighlightLayer { get; set; }

    public GridStats Stats { get; set; }
}



