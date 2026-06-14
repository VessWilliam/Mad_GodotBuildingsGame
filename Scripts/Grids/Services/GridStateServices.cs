using System;
using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Game.Grids;
using Game.Grids.Services;
using Game.Grids.Services.IServices;

public class GridStateServices : IGridStateService
{

    public GridStats Stats { get; } = new();

    private IGridTile _tileServices;
    private readonly GridCache _cache;
    private readonly Func<IEnumerable<BuildingComponent>> _getBuildingComponent;


    public GridStateServices(IGridTile tileServices, Func<IEnumerable<BuildingComponent>> getBuildingComponent)
    {
        _tileServices = tileServices;
        _cache = new GridCache(_tileServices);
        _getBuildingComponent = getBuildingComponent;
    }


    public void Recalculate()
    {
        Stats.Clear();
        _cache.ClearCache();

        foreach (var item in _getBuildingComponent())
            UpdateBuildingComponentGridState(item);

    }


    public void UpdateForDisabled(BuildingComponent component) => Recalculate();

    public void UpdateForEnabled(BuildingComponent component) => UpdateBuildingComponentGridState(component);

    public void UpdateForPlacement(BuildingComponent component) => UpdateBuildingComponentGridState(component);

    public void UpdateForDestruction(BuildingComponent component)
    {

        Stats.PlacementOrder.Remove(component);
        Stats.OwnerBuildings.Remove(component);
        _cache.Invalidate(component);
        Recalculate();

    }

    private void UpdateBuildingComponentGridState(BuildingComponent component)
    {
        UpdateDangerOccupiedTiles(component);
        UpdatePlacementBuildableTiles(component);
        UpdateResourceTiles(component);
        UpdateAttckTiles(component);
    }


    private void UpdateAttckTiles(BuildingComponent component)
    {
        if (!component.BuildingResource.IsAttackBuilding()) return;

        var tileArea = component.GetTileArea();
        var attackTiles = _cache.GetCacheRadius(component, tileArea, component.BuildingResource.AttackRadius);
        Stats.AttackTiles.UnionWith(attackTiles);
    }


    private void UpdateDangerOccupiedTiles(BuildingComponent component)
    {
        Stats.OccupiedTiles.UnionWith(component.GetOccupiedCellPositions());

        if (component.IsDisable) return;


        if (!component.BuildingResource.IsDangerBuilding()) return;

        var tileArea = component.GetTileArea();

        var tileRadius = _tileServices.GetPlacementTilesInRadiusList(
            tileArea, component.BuildingResource.DangerRadius).ToHashSet();

        tileRadius.ExceptWith(Stats.OccupiedTiles);
        Stats.EnemyOccupiedTiles.UnionWith(tileRadius);
    }



    private void UpdatePlacementBuildableTiles(BuildingComponent component)
    {
        Stats.OccupiedTiles.UnionWith(component.GetOccupiedCellPositions());

        if (component.BuildingResource.BuildingRadius > 0)
        {
            var tileArea = component.GetTileArea();

            var allTiles = _cache.GetCacheRadius(component, tileArea, component.BuildingResource.BuildingRadius);

            Stats.AllRadiusTiles.UnionWith(allTiles);
            Stats.BuildingRadiusTiles[component] = allTiles;
            Stats.BuildableTiles.UnionWith(allTiles);
        }

        Stats.BuildableTiles.ExceptWith(Stats.OccupiedTiles);
        Stats.AttackBuildableTiles.UnionWith(Stats.BuildableTiles);
        Stats.BuildableTiles.ExceptWith(Stats.EnemyOccupiedTiles);
    }


    private void UpdateResourceTiles(BuildingComponent component)
    {
        var tileArea = component.GetTileArea();

        var attackTiles = _cache.GetCacheRadius(component,
        tileArea, component.BuildingResource.ResourceRadius);

        Stats.AllRadiusTiles.UnionWith(attackTiles);
    }
}