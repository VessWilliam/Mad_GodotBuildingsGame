using Godot;
using Game.Resources;

namespace Game.Feature.Buildings.Services.IServices;

public interface IBuildingPlacement
{
    void StartPlacement(BuildingResource resource);

    void UpdateMousePosition(Vector2I position);

    void UpdateGridDisplay();

    void CancelPlacement();

    int ConfrimPlacement();

    int GetPlacementCost();

    Rect2I GetHoverGridArea();

    bool IsConfirmPlacement();

}
