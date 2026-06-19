using System;
using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Game.Grids.Services.IServices;
using Godot;

namespace Game.Grids.Services;

public class GridStateServices : IGridStateService
{
    public GridStats Stats { get; } = new();

    private readonly IGridTile _tileServices;
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
        GD.Print("Recalculate called");

        Stats.Clear();
        _cache.ClearCache();

        // Clean up BuildingRadiusTiles before rebuilding
        Stats.BuildingRadiusTiles.Clear();

        // Only get valid (not disposed) building components
        var validComponents = _getBuildingComponent()
            .Where(c => c != null && GodotObject.IsInstanceValid(c))
            .ToList();

        GD.Print($"  Found {validComponents.Count} valid components");

        foreach (var component in validComponents)
        {
            // Double-check component is still valid
            if (!GodotObject.IsInstanceValid(component))
                continue;

            Stats.PlacementOrder.Add(component);

            if (!component.BuildingResource.IsBase &&
                component.BuildingResource.ResourceRadius > 0)
            {
                Stats.OwnerBuildings.Add(component);
            }

            UpdateBuildingComponentGridState(component);
        }

        GD.Print($"Recalculate complete. ResourceTiles: {Stats.ResourceTiles.Count}, Towers: {Stats.PlacementOrder.Count(b => !b.BuildingResource.IsAttackBuilding() && !b.BuildingResource.IsBase && b.BuildingResource.ResourceRadius == 0)}");
    }

    public void UpdateForDisabled(BuildingComponent component)
    {
        if (component is null || !GodotObject.IsInstanceValid(component)) return;
        Recalculate();
    }

    public void UpdateForEnabled(BuildingComponent component)
    {
        if (component is null || !GodotObject.IsInstanceValid(component)) return;
        UpdateBuildingComponentGridState(component);
    }

    public void UpdateForPlacement(BuildingComponent component)
    {
        if (component is null || !GodotObject.IsInstanceValid(component)) return;

        GD.Print($"UpdateForPlacement called for {component.BuildingResource.DisplayName}");

        Stats.PlacementOrder.Add(component);

        if (!component.BuildingResource.IsBase &&
            component.BuildingResource.ResourceRadius > 0)
        {
            Stats.OwnerBuildings.Add(component);
            GD.Print($"  Added to OwnerBuildings. Count: {Stats.OwnerBuildings.Count}");
        }

        UpdateBuildingComponentGridState(component);

        GD.Print($"  After UpdateBuildingComponentGridState - ResourceTiles count: {Stats.ResourceTiles.Count}");
    }

    public void UpdateForDestruction(BuildingComponent component)
    {
        if (component == null || !GodotObject.IsInstanceValid(component))
            return;

        GD.Print($"UpdateForDestruction called for {component.BuildingResource.DisplayName}");

        // Remove from collections
        Stats.PlacementOrder.Remove(component);
        Stats.OwnerBuildings.Remove(component);

        // Clean up any null or disposed entries
        Stats.OwnerBuildings.RemoveAll(v => v == null || !GodotObject.IsInstanceValid(v));
        Stats.PlacementOrder.RemoveAll(b => b == null || !GodotObject.IsInstanceValid(b));

        // Clean up BuildingRadiusTiles for disposed components
        var keysToRemove = Stats.BuildingRadiusTiles.Keys
            .Where(k => k == null || !GodotObject.IsInstanceValid(k))
            .ToList();

        foreach (var key in keysToRemove)
        {
            Stats.BuildingRadiusTiles.Remove(key);
            GD.Print($"  Removed stale BuildingRadiusTiles entry");
        }

        // Remove from cache
        _cache.Invalidate(component);

        // Recalculate with remaining valid components
        Recalculate();
    }

    private void UpdateBuildingComponentGridState(BuildingComponent component)
    {
        // Guard against disposed components
        if (component == null || !GodotObject.IsInstanceValid(component))
            return;

        UpdateDangerOccupiedTiles(component);
        UpdatePlacementBuildableTiles(component);
        UpdateResourceTiles(component);
        UpdateAttackTiles(component);
    }

    private void UpdateAttackTiles(BuildingComponent component)
    {
        if (component == null || !GodotObject.IsInstanceValid(component)) return;
        if (!component.BuildingResource.IsAttackBuilding()) return;

        var tileArea = component.GetTileArea();
        var attackTiles = _cache.GetCacheRadius(
            component,
            tileArea,
            component.BuildingResource.AttackRadius,
            RadiusType.Attack);

        Stats.AttackTiles.UnionWith(attackTiles);
    }

    private void UpdateDangerOccupiedTiles(BuildingComponent component)
    {
        if (component == null || !GodotObject.IsInstanceValid(component)) return;

        Stats.OccupiedTiles.UnionWith(component.GetOccupiedCellPositions());

        if (component.IsDisable) return;

        if (!component.BuildingResource.IsDangerBuilding()) return;

        var tileArea = component.GetTileArea();

        var tileRadius = _cache.GetCacheRadius(
            component,
            tileArea,
            component.BuildingResource.DangerRadius,
            RadiusType.Danger);

        tileRadius.ExceptWith(Stats.OccupiedTiles);
        Stats.EnemyOccupiedTiles.UnionWith(tileRadius);
    }

    private void UpdatePlacementBuildableTiles(BuildingComponent component)
    {
        if (component == null || !GodotObject.IsInstanceValid(component)) return;

        Stats.OccupiedTiles.UnionWith(component.GetOccupiedCellPositions());

        if (component.BuildingResource.BuildingRadius > 0)
        {
            var tileArea = component.GetTileArea();

            var validTiles = _cache.GetCacheRadius(
                component,
                tileArea,
                component.BuildingResource.BuildingRadius,
                RadiusType.Building);

            var allTiles = _cache.GetCacheRadius(
                component,
                tileArea,
                component.BuildingResource.BuildingRadius,
                RadiusType.All);

            Stats.AllRadiusTiles.UnionWith(allTiles);
            Stats.BuildingRadiusTiles[component] = validTiles;
            Stats.BuildableTiles.UnionWith(validTiles);
        }

        Stats.BuildableTiles.ExceptWith(Stats.OccupiedTiles);
        Stats.AttackBuildableTiles.UnionWith(Stats.BuildableTiles);
        Stats.BuildableTiles.ExceptWith(Stats.EnemyOccupiedTiles);
    }

    private void UpdateResourceTiles(BuildingComponent component)
    {
        if (component.BuildingResource.ResourceRadius <= 0)
            return;

        var tileArea = component.GetTileArea();

        GD.Print($"UpdateResourceTiles called for {component.BuildingResource.DisplayName}");
        GD.Print($"  ResourceRadius: {component.BuildingResource.ResourceRadius}");
        GD.Print($"  TileArea: {tileArea}");

        var resourceTiles = _cache.GetCacheRadius(
            component,
            tileArea,
            component.BuildingResource.ResourceRadius,
            RadiusType.Resource);

        GD.Print($"  Found {resourceTiles.Count} resource tiles");

        var oldCount = Stats.ResourceTiles.Count;
        Stats.ResourceTiles.UnionWith(resourceTiles);
        GD.Print($"  ResourceTiles count: {oldCount} -> {Stats.ResourceTiles.Count}");
    }
}