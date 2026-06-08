

using System.Linq;
using Game.Buildings.Contexts;
using Game.Buildings.Services.IServices;
using Game.Component;
using Godot;

namespace Game.Buildings.Services;


public class BuildingRemoveService : IBuildingRemove
{
    private readonly BuildingRemoveContext _context;

    public BuildingRemoveService(BuildingRemoveContext context) => _context = context;

    public bool IsRemove(Vector2I rootCell, out int refundCost)
    {
        refundCost = 0;

        var building = BuildingComponent
        .GetValidBuildingComponents(_context.RootScene)
       .FirstOrDefault(b => 
        b.BuildingResource.IsDeletable &&
        b.IsTileInBuildingArea(rootCell));

        if (building is null) return false;

        if (!_context.GridManager.CanDestroyBuilding(building)) return false;

        refundCost = building.BuildingResource.ResourceCost;

        _context.GridManager.DestroyBuilding(building);

        return true;
    }
}




