

using System.Linq;
using Game.Feature.Buildings.Services.IServices;
using Game.Component;
using Game.Feature.Grids;
using Godot;
using Game.Feature.FloatingTexts;

namespace Game.Feature.Buildings.Services;


public class BuildingRemoveService : IBuildingRemove
{
    private GridManager _gridManager { get; set; }

    private Node _rootScene { get; set; }

    public BuildingRemoveService(GridManager gridManager, Node rootScene)
    {
        _gridManager = gridManager;
        _rootScene = rootScene;
    }

    public bool IsRemove(Vector2I rootCell, out int refundCost, out int instanceId)
    {
        refundCost = 0;
        instanceId = -1;

        var building = BuildingComponent
        .GetValidBuildingComponents(_rootScene)
       .FirstOrDefault(b =>
        b.BuildingResource.IsDeletable &&
        b.IsTileInBuildingArea(rootCell));

        if (building is null) return false;

        if (!_gridManager.CanDestroyBuilding(building))
        {

           FloatingTextManager.ShowMessage($"Can't destroy {building.BuildingResource.DisplayName}");
           return false;
        }

        refundCost = building.BuildingResource.ResourceCost;
        instanceId = (int)building.Owner.GetInstanceId();
        _gridManager.DestroyBuilding(building);

        return true;
    }
}




