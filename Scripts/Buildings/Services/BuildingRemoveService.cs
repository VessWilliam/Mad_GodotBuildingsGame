

using System.Linq;
using Game.Buildings.Services.IServices;
using Game.Component;
using Game.Grids;
using Godot;

namespace Game.Buildings.Services;


public class BuildingRemoveService : IBuildingRemove
{
    private GridManager _gridManager { get; set; }

    private Node _rootScene { get; set; }

    public BuildingRemoveService(GridManager gridManager, Node rootScene)
    {
        _gridManager = gridManager;
        _rootScene = rootScene;
    }

    public bool IsRemove(Vector2I rootCell, out int refundCost)
    {
        refundCost = 0;

        var building = BuildingComponent
        .GetValidBuildingComponents(_rootScene)
       .FirstOrDefault(b =>
        b.BuildingResource.IsDeletable &&
        b.IsTileInBuildingArea(rootCell));

        if (building is null) return false;

        if (!_gridManager.CanDestroyBuilding(building)) return false;

        refundCost = building.BuildingResource.ResourceCost;

        _gridManager.DestroyBuilding(building);

        return true;
    }
}




