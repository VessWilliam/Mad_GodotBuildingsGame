

using Game.Feature.Grids.Services.IServices;
using Godot;

namespace Game.Feature.Grids.Services;

public class GridMouseControlService : IGridMouseControl
{
    private readonly TileMapLayer _highlightTilemapLayer;

    public GridMouseControlService(TileMapLayer referenceLayer) => _highlightTilemapLayer = referenceLayer;

    public Vector2I WorldPositionToTilePosition(Vector2 worldPosition)
    {
        var tilePosition = worldPosition / 64;
        tilePosition = tilePosition.Floor();
        return new((int)tilePosition.X, (int)tilePosition.Y);
    }

    public Vector2I MouseGridCellPosition()
    {
        var mouse = _highlightTilemapLayer.GetLocalMousePosition();
        return WorldPositionToTilePosition(mouse);
    }

    public Vector2I MouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
    {
        var mouseGridPosition = _highlightTilemapLayer.GetLocalMousePosition() / 64;
        mouseGridPosition -= dimensions / 2;
        mouseGridPosition = mouseGridPosition.Round();
        return new((int)mouseGridPosition.X, (int)mouseGridPosition.Y);
    }
}
