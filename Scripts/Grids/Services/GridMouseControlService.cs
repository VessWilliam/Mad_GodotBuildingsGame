

using Game.Grids.Services.IServices;
using Godot;

namespace Game.Grids.Services;

public class GridMouseControlService : IGridMouseControl
{
    private readonly TileMapLayer _referenceLayer;

    public GridMouseControlService(TileMapLayer referenceLayer) => _referenceLayer = referenceLayer;

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
    {
        var tilePosition = worldPosition / 64;
        tilePosition = tilePosition.Floor();
        return new((int)tilePosition.X, (int)tilePosition.Y);
    }

    public Vector2I GetMouseGridCellPosition()
    {
        var mouse = _referenceLayer.GetLocalMousePosition();
        return ConvertWorldPositionToTilePosition(mouse);
    }

    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
    {
        var mouseGridPosition = _referenceLayer.GetLocalMousePosition();
        mouseGridPosition -= dimensions / 2;
        mouseGridPosition = mouseGridPosition.Round();
        return new((int)mouseGridPosition.X, (int)mouseGridPosition.Y);
    }
}
