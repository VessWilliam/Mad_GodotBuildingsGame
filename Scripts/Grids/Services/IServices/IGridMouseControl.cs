using Godot;

namespace Game.Grids.Services.IServices;

public interface IGridMouseControl
{
    Vector2I GetMouseGridCellPosition();
    
    Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions);

    Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition);
}
