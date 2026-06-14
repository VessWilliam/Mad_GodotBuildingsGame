using Game.Autoload;
using Game.Component;
using Game.Extentions;
using Game.Grids.Services;
using Game.Grids.Services.IServices;
using Game.Utils;
using Games.Grids.Services.IServices;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Grids;

public partial class GridManager : Node
{

    [Signal]
    public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);

    [Signal]
    public delegate void GridStateUpdatedEventHandler();

    [Export]
    private TileMapLayer highlightTilemapLayer;

    [Export]
    private TileMapLayer baseTerrainTilemapLayer;

    private readonly GridStats _stats = new();

    private GridCache _cache;

    private IGridTile _tileService;

    private IGridHighlight _highlightService;

    private IGridMouseControl _mouseControlService;

    private List<TileMapLayer> allTilemapLayers = new();

    private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();

    public override void _Ready()
    {
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDisable, Callable.From<BuildingComponent>(OnBuildingDisable));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingEnable, Callable.From<BuildingComponent>(OnBuildingEnable));
        allTilemapLayers = GetAllTilemapLayers(baseTerrainTilemapLayer);


        MapTileMapLayersToElevationLayers();

        _tileService = new GridTileServices(_stats, allTilemapLayers, tileMapLayerToElevationLayer);

        _highlightService = new GridHighlightService(highlightTilemapLayer,
            _stats,
            _tileService.GetPlacementTilesInRadiusList,
            _tileService.GetResourceTilesInRadiusList);

        _mouseControlService = new GridMouseControlService(highlightTilemapLayer);

        _cache = new GridCache(_tileService);
    }

    public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition) =>
     _stats.AllRadiusTiles.Contains(tilePosition);

    public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false) => _tileService.IsTileAreaBuildable(tileArea, isAttackTiles);

    public void DisplayEnemyOccupiedTiles() => _highlightService.HighlightEnemyOccupiedTiles();

    public void DisplayBuildableTiles(bool isAttackTiles = false) => _highlightService.HighlightBuildableTiles(isAttackTiles);

    public void DisplayExpandTiles(Rect2I tileArea, int radius) => _highlightService.HighlightExpandTiles(tileArea, radius);

    public void DisplayAttackTiles(Rect2I tileArea, int radius) => _highlightService.HighlightAttackTiles(tileArea, radius);

    public void DisplayResourceTiles(Rect2I tileArea, int radius) => _highlightService.HighlightResourceTiles(tileArea, radius);

    public void ClearHighlightedTiles() => _highlightService.ClearHighlightTile();

    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions) => _mouseControlService.MouseGridCellPositionWithDimensionOffset(dimensions);

    public Vector2I GetMouseGridCellPosition() => _mouseControlService.MouseGridCellPosition();

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition) => _mouseControlService.WorldPositionToTilePosition(worldPosition);

    public bool CanDestroyBuilding(BuildingComponent component)
    {
        // Base is permanent, never deletable
        if (component.BuildingResource.IsBase)
            return false;

        // Villages are always freely deletable
        if (component.BuildingResource.ResourceRadius > 0)
            return true;


        if (component.BuildingResource.IsAttackBuilding())
        {
            var tileArea = component.GetTileArea();
            var attackArea = _cache.GetCacheRadius(component, tileArea, component.BuildingResource.AttackRadius)
                .ToHashSet();

            var buildingsInRadius = _stats.PlacementOrder.Where(b =>
                !b.BuildingResource.IsBase &&
                !b.BuildingResource.IsAttackBuilding() &&
                !b.BuildingResource.IsDangerBuilding() &&
                b.GetOccupiedCellPositions().Any(attackArea.Contains)
            ).ToList();

            foreach (var b in buildingsInRadius)
                GD.Print($"  Barracks blocked by: {b.BuildingResource.DisplayName} at {b.GetGridCellPosition()}");

            return buildingsInRadius.Count == 0;
        }

        // Tower — grouped to village if both radii overlap bidirectionally
        var towerArea = component.GetTileArea();
        var towerRadius = _stats.BuildingRadiusTiles[component];

        var hasBarrackInRadius = _stats.PlacementOrder.Any(b =>
            b.BuildingResource.IsAttackBuilding() &&
            b.GetOccupiedCellPositions().Any(towerRadius.Contains)
        );

        GD.Print($"  hasBarrackInRadius: {hasBarrackInRadius}");

        if (hasBarrackInRadius)
        {
            var orphanTowers = _stats.PlacementOrder
                .Where(b => !b.BuildingResource.IsAttackBuilding() &&
                            !b.BuildingResource.IsBase &&
                            b.BuildingResource.ResourceRadius == 0)
                .ToList();
            int orphanIndex = orphanTowers.IndexOf(component);
            return orphanIndex == orphanTowers.Count - 1;
        }


        var isGroupedToVillage = _stats.OwnerBuildings.Any(v =>
        {
            var villageArea = v.GetTileArea();
            var towerReachesVillage = v.GetOccupiedCellPositions().Any(towerRadius.Contains);
            var villageRadius = _tileService.GetTileInRadius(villageArea, v.BuildingResource.ResourceRadius, (_) => true).ToHashSet();
            var villageReachesTower = component.GetOccupiedCellPositions().Any(villageRadius.Contains);
            GD.Print($"  village: {v.GetGridCellPosition()} | towerReachesVillage: {towerReachesVillage} | villageReachesTower: {villageReachesTower}");
            return towerReachesVillage && villageReachesTower;
        });

        GD.Print($"  isGroupedToVillage: {isGroupedToVillage} | villages count: {_stats.OwnerBuildings.Count}");


        // Towers locked while any village exists
        if (isGroupedToVillage)
            return false;

        // No villages → towers follow LIFO
        int index = _stats.PlacementOrder.IndexOf(component);
        return index == _stats.PlacementOrder.Count - 1;
    }

    public void DestroyBuilding(BuildingComponent component)
    {
        _stats.PlacementOrder.Remove(component);
        component.Destroy();
    }

    private List<TileMapLayer> GetAllTilemapLayers(Node2D rootNode)
    {
        var result = new List<TileMapLayer>();
        var children = rootNode.GetChildren();
        children.Reverse();
        foreach (var child in children)
        {
            if (child is Node2D childNode)
            {
                result.AddRange(GetAllTilemapLayers(childNode));
            }
        }

        if (rootNode is TileMapLayer tileMapLayer)
        {
            result.Add(tileMapLayer);
        }
        return result;
    }

    private void MapTileMapLayersToElevationLayers()
    {
        foreach (var layer in allTilemapLayers)
        {
            ElevationLayer elevationLayer;
            Node startNode = layer;
            do
            {
                var parent = startNode.GetParent();
                elevationLayer = parent as ElevationLayer;
                startNode = parent;
            } while (elevationLayer == null && startNode != null);

            tileMapLayerToElevationLayer[layer] = elevationLayer;
        }
    }

    private void UpdateGoblinOccupiedTiles(BuildingComponent buildingComponent)
    {
        _stats.OccupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());

        if (buildingComponent.IsDisable) return;

        var tileArea = buildingComponent.GetTileArea();
        if (buildingComponent.BuildingResource.IsDangerBuilding())
        {
            var tilesInRadius = _tileService.GetPlacementTilesInRadiusList(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();
            tilesInRadius.ExceptWith(_stats.OccupiedTiles);
            _stats.EnemyOccupiedTiles.UnionWith(tilesInRadius);
        }
    }

    private void UpdateValidBuildableTiles(BuildingComponent component)
    {
        _stats.OccupiedTiles.UnionWith(component.GetOccupiedCellPositions());
        var tileArea = component.GetTileArea();

        if (component.BuildingResource.BuildingRadius > 0)
        {
            var allTiles = _tileService.GetTileInRadius(tileArea, component.BuildingResource.BuildingRadius, (_) => true);
            _stats.AllRadiusTiles.UnionWith(allTiles);

            var validTiles = _tileService.GetPlacementTilesInRadiusList(tileArea, component.BuildingResource.BuildingRadius);
            _stats.BuildingRadiusTiles[component] = validTiles.ToHashSet();
            _stats.BuildableTiles.UnionWith(validTiles);
        }

        _stats.BuildableTiles.ExceptWith(_stats.OccupiedTiles);
        _stats.AttackBuildableTiles.UnionWith(_stats.BuildableTiles);

        _stats.BuildableTiles.ExceptWith(_stats.EnemyOccupiedTiles);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent component)
    {
        var tileArea = component.GetTileArea();
        var resourceTiles = _tileService.GetResourceTilesInRadiusList(tileArea, component.BuildingResource.ResourceRadius);

        var oldResourceTileCount = _stats.ResourceTiles.Count;
        _stats.ResourceTiles.UnionWith(resourceTiles);

        if (oldResourceTileCount != _stats.ResourceTiles.Count)
        {
            EmitSignal(SignalName.ResourceTilesUpdated, _stats.ResourceTiles.Count);
        }
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateAttackTiles(BuildingComponent component)
    {
        if (!component.BuildingResource.IsAttackBuilding()) return;

        var tileArea = component.GetTileArea();
        var newAttackTiles = _cache.GetCacheRadius(component, tileArea, component.BuildingResource.AttackRadius)
            .ToHashSet();
        _stats.AttackTiles.UnionWith(newAttackTiles);
    }

    private void UpdateBuildingComponentGridState(BuildingComponent component)
    {
        UpdateGoblinOccupiedTiles(component);
        UpdateValidBuildableTiles(component);
        UpdateCollectedResourceTiles(component);
        UpdateAttackTiles(component);
    }


    private void RecalculateGrid()
    {
        _stats.Clear();

        var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);

        foreach (var buildingComponent in buildingComponents)
        {
            UpdateBuildingComponentGridState(buildingComponent);
        }

        CheckGoblinCampDestruction();

        EmitSignal(SignalName.ResourceTilesUpdated, _stats.ResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void RecalculateGoblinOccupiedTiles()
    {
        _stats.EnemyOccupiedTiles.Clear();
        var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
        foreach (var building in dangerBuildings)
        {
            UpdateGoblinOccupiedTiles(building);
        }
    }

    private void CheckGoblinCampDestruction()
    {
        var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
        foreach (var building in dangerBuildings)
        {
            var tileArea = building.GetTileArea();
            var isInsideAttackTile = tileArea.ToTiles().Any(_stats.AttackTiles.Contains);
            if (isInsideAttackTile) building.Disable();
            else building.Enable();
        }
    }

    private void OnBuildingPlaced(BuildingComponent component)
    {
        _stats.PlacementOrder.Add(component);

        if (!component.BuildingResource.IsBase &&
            component.BuildingResource.ResourceRadius > 0)
            _stats.OwnerBuildings.Add(component);

        UpdateBuildingComponentGridState(component);
        CheckGoblinCampDestruction();
    }

    private void OnBuildingDestroyed(BuildingComponent component)
    {
        GD.Print($"BuildingDestroyed signal: {component.BuildingResource.DisplayName}");

        _stats.PlacementOrder.Remove(component);
        _stats.OwnerBuildings.Remove(component);
        RecalculateGrid();

        GD.Print($"Buildings in dictionary: {_stats.BuildingRadiusTiles.Count}");
    }

    private void OnBuildingEnable(BuildingComponent component) => UpdateBuildingComponentGridState(component);


    private void OnBuildingDisable(BuildingComponent component) => RecalculateGrid();
}