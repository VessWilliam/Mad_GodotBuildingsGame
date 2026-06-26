using Godot;

namespace Game.Feature.Grids.Services.IServices;

public interface IGridMouseControl
{
    Vector2I MouseGridCellPosition();
    
    Vector2I MouseGridCellPositionWithDimensionOffset(Vector2 dimensions);

    Vector2I WorldPositionToTilePosition(Vector2 worldPosition);
}
