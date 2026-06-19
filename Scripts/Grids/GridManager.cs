using Game.Autoload;
using Game.Component;
using Game.Extentions;
using Game.Grids.Services;
using Game.Grids.Services.IServices;
using Game.Utils;
using Games.Grids.Services.IServices;
using Godot;
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

    private IGridStateService _gridState;
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

        _tileService = new GridTileServices(allTilemapLayers, tileMapLayerToElevationLayer);

        _gridState = new GridStateServices(
            _tileService,
            () => BuildingComponent.GetValidBuildingComponents(this)
        );

        _highlightService = new GridHighlightService(
            highlightTilemapLayer,
            _gridState.Stats,
            _tileService.GetPlacementTilesInRadiusList,
            _tileService.GetResourceTilesInRadiusList
        );

        _mouseControlService = new GridMouseControlService(highlightTilemapLayer);
    }

    private bool IsInitialized() => _gridState != null && _tileService != null;

    public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition)
    {
        if (!IsInitialized()) return false;
        return _gridState.Stats.AllRadiusTiles.Contains(tilePosition);
    }

    public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false)
    {
        if (!IsInitialized()) return false;
        return _tileService.IsTileAreaBuildable(
            tileArea,
            _gridState.Stats.GetBuildableTileSet(isAttackTiles),
            _gridState.Stats.OccupiedTiles,
            isAttackTiles
        );
    }

    public bool CanDestroyBuilding(BuildingComponent component)
    {
        // Guard against disposed components
        if (component == null || !GodotObject.IsInstanceValid(component))
            return false;

        // Base is permanent, never deletable
        if (component.BuildingResource.IsBase)
            return false;

        // Villages are always freely deletable
        if (component.BuildingResource.ResourceRadius > 0)
            return true;

        // Barracks (Attack Building) logic
        if (component.BuildingResource.IsAttackBuilding())
        {
            var tileArea = component.GetTileArea();
            var attackArea = _gridState.Stats.AttackTiles;

            // Barracks can only be deleted if there are NO buildings in its attack radius
            var buildingsInRadius = _gridState.Stats.PlacementOrder.Where(b =>
                b != component && // Don't count itself
                b != null &&
                GodotObject.IsInstanceValid(b) &&
                !b.BuildingResource.IsBase &&
                !b.BuildingResource.IsAttackBuilding() &&
                !b.BuildingResource.IsDangerBuilding() &&
                b.GetOccupiedCellPositions().Any(attackArea.Contains)
            ).ToList();

            foreach (var b in buildingsInRadius)
                GD.Print($"  Barracks blocked by: {b.BuildingResource.DisplayName} at {b.GetGridCellPosition()}");

            return buildingsInRadius.Count == 0;
        }

        // Tower logic
        // Check if tower has BuildingRadiusTiles entry
        if (!_gridState.Stats.BuildingRadiusTiles.TryGetValue(component, out var towerRadius))
        {
            GD.Print($"  Tower has no BuildingRadiusTiles entry - Can delete");
            return true;
        }

        // Check if any barracks are in the tower's radius
        var hasBarrackInRadius = _gridState.Stats.PlacementOrder.Any(b =>
            b != null &&
            GodotObject.IsInstanceValid(b) &&
            b.BuildingResource.IsAttackBuilding() &&
            b.GetOccupiedCellPositions().Any(towerRadius.Contains)
        );

        GD.Print($"  hasBarrackInRadius: {hasBarrackInRadius}");

        if (hasBarrackInRadius)
        {
            // If barracks exist, towers follow LIFO (only the last tower can be deleted)
            var orphanTowers = _gridState.Stats.PlacementOrder
                .Where(b => b != null &&
                           GodotObject.IsInstanceValid(b) &&
                           !b.BuildingResource.IsAttackBuilding() &&
                           !b.BuildingResource.IsBase &&
                           b.BuildingResource.ResourceRadius == 0)
                .Distinct()
                .ToList();

            int orphanIndex = orphanTowers.IndexOf(component);
            bool canDelete = orphanIndex == orphanTowers.Count - 1;
            GD.Print($"  Barracks exist - Tower LIFO: {canDelete} (index {orphanIndex} of {orphanTowers.Count})");
            return canDelete;
        }

        // Check if tower is grouped to any village (bidirectional radius overlap)
        var isGroupedToVillage = _gridState.Stats.OwnerBuildings.Any(v =>
        {
            if (v == null || !GodotObject.IsInstanceValid(v))
                return false;

            var villageArea = v.GetTileArea();
            var towerReachesVillage = v.GetOccupiedCellPositions().Any(towerRadius.Contains);
            var villageRadius = _tileService.GetTileInRadius(villageArea, v.BuildingResource.ResourceRadius, (_) => true).ToHashSet();
            var villageReachesTower = component.GetOccupiedCellPositions().Any(villageRadius.Contains);
            GD.Print($"  village: {v.GetGridCellPosition()} | towerReachesVillage: {towerReachesVillage} | villageReachesTower: {villageReachesTower}");
            return towerReachesVillage && villageReachesTower;
        });

        GD.Print($"  isGroupedToVillage: {isGroupedToVillage} | villages count: {_gridState.Stats.OwnerBuildings.Count}");

        // Towers locked while any village exists
        if (isGroupedToVillage)
        {
            GD.Print("  Tower is grouped to village - Cannot delete");
            return false;
        }

        // No villages → towers follow LIFO (only the last tower can be deleted)
        var allTowers = _gridState.Stats.PlacementOrder
            .Where(b => b != null &&
                       GodotObject.IsInstanceValid(b) &&
                       !b.BuildingResource.IsAttackBuilding() &&
                       !b.BuildingResource.IsBase &&
                       b.BuildingResource.ResourceRadius == 0)
            .Distinct()
            .ToList();

        int towerIndex = allTowers.IndexOf(component);
        bool canDeleteTower = towerIndex == allTowers.Count - 1;

        GD.Print($"  No villages - Tower LIFO: {canDeleteTower} (tower {towerIndex + 1} of {allTowers.Count})");

        return canDeleteTower;
    }
    public void DestroyBuilding(BuildingComponent component)
    {
        if (component == null || !GodotObject.IsInstanceValid(component))
        {
            GD.Print("DestroyBuilding: Invalid component");
            return;
        }

        GD.Print($"DestroyBuilding called for {component.BuildingResource.DisplayName} at {component.GetGridCellPosition()}");

        // Check if we can destroy it first
        if (!CanDestroyBuilding(component))
        {
            GD.Print($"Cannot destroy {component.BuildingResource.DisplayName} - not allowed");
            return;
        }

        component.Destroy();
    }

    public void DisplayEnemyOccupiedTiles() => _highlightService?.HighlightEnemyOccupiedTiles();
    public void DisplayBuildableTiles(bool isAttackTiles = false) => _highlightService?.HighlightBuildableTiles(isAttackTiles);
    public void DisplayExpandTiles(Rect2I tileArea, int radius) => _highlightService?.HighlightExpandTiles(tileArea, radius);
    public void DisplayAttackTiles(Rect2I tileArea, int radius) => _highlightService?.HighlightAttackTiles(tileArea, radius);
    public void DisplayResourceTiles(Rect2I tileArea, int radius) => _highlightService?.HighlightResourceTiles(tileArea, radius);
    public void ClearHighlightedTiles() => _highlightService?.ClearHighlightTile();

    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions) =>
        _mouseControlService?.MouseGridCellPositionWithDimensionOffset(dimensions) ?? Vector2I.Zero;

    public Vector2I GetMouseGridCellPosition() =>
        _mouseControlService?.MouseGridCellPosition() ?? Vector2I.Zero;

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition) =>
        _mouseControlService?.WorldPositionToTilePosition(worldPosition) ?? Vector2I.Zero;

    private List<TileMapLayer> GetAllTilemapLayers(Node2D rootNode)
    {
        var result = new List<TileMapLayer>();
        var children = rootNode.GetChildren();
        children.Reverse();
        foreach (var child in children)
        {
            if (child is Node2D childNode)
                result.AddRange(GetAllTilemapLayers(childNode));
        }

        if (rootNode is TileMapLayer tileMapLayer)
            result.Add(tileMapLayer);

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

    private void CheckEnemyBuildingDestruction()
    {
        if (!IsInitialized()) return;

        var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
        foreach (var building in dangerBuildings)
        {
            if (building == null || !GodotObject.IsInstanceValid(building))
                continue;

            var tileArea = building.GetOccupiedCellPositions();
            var isInsideAttackTile = tileArea.Any(_gridState.Stats.AttackTiles.Contains);
            if (isInsideAttackTile) building.Disable();
            else building.Enable();
        }
    }

    private void OnBuildingPlaced(BuildingComponent component)
    {
        if (!IsInitialized() || component == null) return;

        _gridState.UpdateForPlacement(component);
        CheckEnemyBuildingDestruction();

        // Emit resource update signal
        EmitSignal(SignalName.ResourceTilesUpdated, _gridState.Stats.ResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);

        GD.Print($"OnBuildingPlaced: ResourceTiles count = {_gridState.Stats.ResourceTiles.Count}");
    }

    private void OnBuildingDestroyed(BuildingComponent component)
    {
        if (!IsInitialized()) return;

        GD.Print($"BuildingDestroyed signal: {component?.BuildingResource?.DisplayName ?? "Unknown"}");

        _gridState.UpdateForDestruction(component);
        CheckEnemyBuildingDestruction();

        EmitSignal(SignalName.ResourceTilesUpdated, _gridState.Stats.ResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);

        GD.Print($"Buildings in dictionary: {_gridState.Stats.BuildingRadiusTiles.Count}");
    }

    private void OnBuildingEnable(BuildingComponent component)
    {
        if (!IsInitialized() || component == null) return;

        _gridState.UpdateForEnabled(component);
        CheckEnemyBuildingDestruction();
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void OnBuildingDisable(BuildingComponent component)
    {
        if (!IsInitialized() || component == null) return;

        _gridState.UpdateForDisabled(component);
        CheckEnemyBuildingDestruction();

        EmitSignal(SignalName.ResourceTilesUpdated, _gridState.Stats.ResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);
    }
}