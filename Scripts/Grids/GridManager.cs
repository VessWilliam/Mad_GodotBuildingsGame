using Game.Autoload;
using Game.Component;
using Game.Extentions;
using Game.Utils;
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
    }

    public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var layer in allTilemapLayers)
        {
            var customData = layer.GetCellTileData(tilePosition);

            if (customData is null || (bool)customData.GetCustomData(Constants.IS_IGNORE))
                continue;

            return (layer, (bool)customData.GetCustomData(dataName));
        }
        return (null, false);
    }

    public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition) =>
     _stats.AllRadiusTiles.Contains(tilePosition);

    public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false)
    {
        var tiles = tileArea.ToTiles().ToList();
        if (tiles.Count is 0) return false;

        var (firstTileMapLayer, _) = GetTileCustomData(tiles[0], Constants.IS_BUILDABLE);

        var targetElevationLayer = firstTileMapLayer is not null ? tileMapLayerToElevationLayer[firstTileMapLayer] : null;

        var tileSetToCheck = GetBuildableTileSet(isAttackTiles);

        if (isAttackTiles) tileSetToCheck = tileSetToCheck.Except(_stats.OccupiedTiles).ToHashSet();

        return tiles.All((tilePosition) =>
        {
            var (tileMapLayer, isBuildable) = GetTileCustomData(tilePosition, Constants.IS_BUILDABLE);
            var elevationLayer = tileMapLayer is not null ? tileMapLayerToElevationLayer[tileMapLayer] : null;

            return isBuildable && tileSetToCheck.Contains(tilePosition) && elevationLayer == targetElevationLayer;
        });
    }

    public void HighlightGoblinOccupiedTiles()
    {
        var atlasCoords = new Vector2I(2, 0);
        foreach (var tilePosition in _stats.EnemyOccupiedTiles)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightBuildableTiles(bool isAttackTiles = false)
    {
        foreach (var tilePosition in GetBuildableTileSet(isAttackTiles))
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
        }
    }

    public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
    {
        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();

        var expandedTiles = validTiles.Except(_stats.BuildableTiles)
        .Except(_stats.OccupiedTiles);

        var atlasCoords = new Vector2I(1, 0);
        foreach (var tilePosition in expandedTiles)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
        }

    }

    public void HighlightAttackTiles(Rect2I tileArea, int radius)
    {
        var buildingAreaTiles = tileArea.ToTiles();
        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet()
            .Except(_stats.AttackBuildableTiles)
            .Except(buildingAreaTiles);

        var atlasCoords = new Vector2I(1, 0);
        foreach (var tilePosition in validTiles)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightResourceTiles(Rect2I tileArea, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
        var atlasCoords = new Vector2I(1, 0);
        foreach (var tilePosition in resourceTiles)
        {
            highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void ClearHighlightedTiles() => highlightTilemapLayer.Clear();


    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
    {
        var mouseGridPosition = highlightTilemapLayer.GetGlobalMousePosition() / 64;
        mouseGridPosition -= dimensions / 2;
        mouseGridPosition = mouseGridPosition.Round();
        return new Vector2I((int)mouseGridPosition.X, (int)mouseGridPosition.Y);
    }

    public Vector2I GetMouseGridCellPosition()
    {
        var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();
        return ConvertWorldPositionToTilePosition(mousePosition);
    }

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
    {
        var tilePosition = worldPosition / 64;
        tilePosition = tilePosition.Floor();
        return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);
    }

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
            var attackArea = GetTilesInRadius(tileArea, component.BuildingResource.AttackRadius, (_) => true)
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
        var towerRadius = GetTilesInRadius(towerArea, component.BuildingResource.BuildingRadius, (_) => true)
            .ToHashSet();

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
            var villageRadius = GetTilesInRadius(villageArea, v.BuildingResource.ResourceRadius, (_) => true).ToHashSet();
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

    private HashSet<Vector2I> GetBuildableTileSet(bool isAttackTiles = false) =>
         _stats.GetBuildableTileSet(isAttackTiles);

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
            var tilesInRadius = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();
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
            var allTiles = GetTilesInRadius(tileArea, component.BuildingResource.BuildingRadius, (_) => true);
            _stats.AllRadiusTiles.UnionWith(allTiles);

            var validTiles = GetValidTilesInRadius(tileArea, component.BuildingResource.BuildingRadius);
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
        var resourceTiles = GetResourceTilesInRadius(tileArea, component.BuildingResource.ResourceRadius);

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
        var newAttackTiles = GetTilesInRadius(tileArea, component.BuildingResource.AttackRadius, (_) => true)
            .ToHashSet();
        _stats.AttackTiles.UnionWith(newAttackTiles);
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

    private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
    {
        var distanceX = centerPosition.X - (tilePosition.X + .5);
        var distanceY = centerPosition.Y - (tilePosition.Y + .5);
        var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        return distanceSquared <= radius * radius;
    }

    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
    {
        var result = new List<Vector2I>();
        var tileAreaF = tileArea.ToRect2F();
        var tileAreaCenter = tileAreaF.GetCenter();
        var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;

        for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
        {
            for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
            {
                var tilePosition = new Vector2I(x, y);
                if (!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;
                result.Add(tilePosition);
            }
        }
        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) =>
        {
            return GetTileCustomData(tilePosition, Constants.IS_BUILDABLE).Item2;
        });
    }

    private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) =>
        {
            return GetTileCustomData(tilePosition, Constants.IS_WOOD).Item2;
        });
    }

    private void UpdateBuildingComponentGridState(BuildingComponent component)
    {
        UpdateGoblinOccupiedTiles(component);
        UpdateValidBuildableTiles(component);
        UpdateCollectedResourceTiles(component);
        UpdateAttackTiles(component);
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

    private void OnBuildingEnable(BuildingComponent component)
    {
        UpdateBuildingComponentGridState(component);
    }

    private void OnBuildingDisable(BuildingComponent component)
    {
        RecalculateGrid();
    }
}
