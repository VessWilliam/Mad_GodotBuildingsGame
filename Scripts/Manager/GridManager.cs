using Game.Autoload;
using Game.Component;
using Game.Extentions;
using Game.Generals;
using Game.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Manager;

public partial class GridManager : Node
{
    [Signal]
    public delegate void ResourceTileUpdateEventHandler(int resourceCount);

    [Signal]
    public delegate void GridStateUpdateEventHandler();

    [Export]
    private TileMapLayer highlightTilemapLayer;

    [Export]
    private TileMapLayer terrainTilemapLayer;


    private HashSet<Vector2I> validBuildableArea = new();
    private HashSet<Vector2I> collectedResourceTiles = new();
    private HashSet<Vector2I> allTilesBuildableRadius = new();
    private HashSet<Vector2I> occupiedBuild = new();
   
    private List<TileMapLayer> allTilemapLayer = new();
    private Dictionary<TileMapLayer, ElevationLayer> tileMapElevation = new();

    private bool _initialLoadComplete = false;

    public override void _Ready()
    {

        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced,
         Callable.From<BulidingComponent>(OnBuildingPlaced));

        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed,
         Callable.From<BulidingComponent>(OnBuildingDestroyed));

        allTilemapLayer = GetAllTileMapLayer(terrainTilemapLayer);

        MapTileMapElevationLayer();
        Callable.From(() => _initialLoadComplete = true).CallDeferred();
    }

    public void ClearHighlightArea() => highlightTilemapLayer.Clear();


    public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var item in allTilemapLayer)
        {
            var customData = item.GetCellTileData(tilePosition);

            if (customData is null || (bool)customData.GetCustomData(Constants.IS_IGNORE)) continue;
            return (item, (bool)customData.GetCustomData(dataName));
        }

        return (null, false);
    }

    public bool IsTilePositionBuildable(Vector2I tilePos) => validBuildableArea.Contains(tilePos);
    
    public bool IsTilePositionInAnyBuildableRadius(Vector2I tilePos) => allTilesBuildableRadius.Contains(tilePos);

    public bool IsTileAreaBuildable(Rect2I tileArea)
    {

        var tiles = tileArea.ToTiles().ToList();

        if (tiles.Count == 0)
            return false;

        (TileMapLayer firstlayer, bool _) = GetTileCustomData(tiles[0], Constants.IS_BUILDABLE);

        var targetelevationLayer = firstlayer is null ? null : tileMapElevation[firstlayer];

        return tiles.All(t =>
        {
            (TileMapLayer layer, bool isBuildable) = GetTileCustomData(t, Constants.IS_BUILDABLE);
            var elevationLayer = layer is null ? null : tileMapElevation[layer];
            return isBuildable && validBuildableArea.Contains(t) && elevationLayer == targetelevationLayer;
        });

    }


    public void HighlightBuildArea()
    {
        foreach (var tilePos in validBuildableArea)
        {
            highlightTilemapLayer.SetCell(tilePos, 0, Vector2I.Zero);
        }
    }

    public void HighlightExpandBuildArea(Rect2I tileArea, int radius)
    {

        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();

        var expandedTiles = validTiles.Except(validBuildableArea).Except(occupiedBuild);

        var atlasCoords = new Vector2I(1, 0);

        foreach (var item in expandedTiles)
        {
            highlightTilemapLayer.SetCell(item, 0, atlasCoords);
        }
    }

    public void HighlightResourceArea(Rect2I tileArea, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(tileArea, radius);

        var atlasCoords = new Vector2I(1, 0);

        foreach (var item in resourceTiles)
        {
            highlightTilemapLayer.SetCell(item, 0, atlasCoords);
        }

    }

    public Vector2I GetMouseGridCellPositionWithOffset(Vector2 demensions)
    {
        var mousePos = highlightTilemapLayer.GetGlobalMousePosition() / 64;
        
        mousePos -= demensions / 2;
        
        mousePos = mousePos.Round();

        return new Vector2I((int)mousePos.X, (int)mousePos.Y);

    }


    public Vector2I GetMouseGridCellPosition()
    {
        var mousePos = highlightTilemapLayer.GetGlobalMousePosition();

        return ConvertWorldtoTilePosition(mousePos);
    }

    public Vector2I ConvertWorldtoTilePosition(Vector2 worldPos)
    {
        var tilePos = (worldPos / 64).Floor();
        
        return new((int)tilePos.X, (int)tilePos.Y);
    }

    private void UpdateCollectResourceArea(BulidingComponent buildingComponent, bool emitSignal = true)
    {

        var rootCell = buildingComponent.GetGridCellPosition();

        int radius = buildingComponent.BuildingResource.ResourceRadius;
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        var resourceTiles = GetResourceTilesInRadius(tileArea, radius);

        var oldResourceTileCount = collectedResourceTiles.Count;

        collectedResourceTiles.UnionWith(resourceTiles);

        if (emitSignal && oldResourceTileCount != collectedResourceTiles.Count)
        {
            EmitSignal(SignalName.ResourceTileUpdate, collectedResourceTiles.Count);
        }


        if (emitSignal) EmitSignal(SignalName.GridStateUpdate);
    }


    private void UpdateValidBuildArea(
     BulidingComponent buildingComponent,
     bool emitSignal = true)
    {

        occupiedBuild.UnionWith(buildingComponent.GetOccupiedCellPosition());
        var rootCell = buildingComponent.GetGridCellPosition();
        int radius = buildingComponent.BuildingResource.BuildingRadius;
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        
        var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildingRadius, (_) => true);
        allTilesBuildableRadius.UnionWith(allTiles);

        var validTiles = GetValidTilesInRadius(tileArea, radius);
        validBuildableArea.UnionWith(validTiles);


        validBuildableArea.ExceptWith(occupiedBuild);

        if (emitSignal) EmitSignal(SignalName.GridStateUpdate);
    }

    private void RecalculateBuildArea(BulidingComponent excludeComponent)
    {
        occupiedBuild.Clear();
        validBuildableArea.Clear();
        collectedResourceTiles.Clear();
        allTilesBuildableRadius.Clear();

        var buildingComponenets = GetTree()
        .GetNodesInGroup(nameof(BulidingComponent))
        .Cast<BulidingComponent>().Where(b => b != excludeComponent)
        .ToList();

        GD.Print($"Recalculating with {buildingComponenets.Count} buildings");

        foreach (var item in buildingComponenets)
        {
            GD.Print($"Building at grid pos: {item.GetGridCellPosition()}, GlobalPos: {item.GlobalPosition}");
            var resourceRadius = item.BuildingResource.ResourceRadius;
            GD.Print($"Resource radius: {resourceRadius}");  // ADD THIS
            UpdateValidBuildArea(item, false);
            UpdateCollectResourceArea(item, false);
        }
        GD.Print($"Collected resource tiles after loop: {collectedResourceTiles.Count}");

        EmitSignal(SignalName.ResourceTileUpdate, collectedResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdate);
    }


    private bool IsTileInsideCircle(Vector2 center, Vector2 tilePos, float radius)
    {
        var distX = center.X - (tilePos.X + .5);
        var distY = center.Y - (tilePos.Y + .5);
        var distSquared = (distX * distX) + (distY * distY);
        return distSquared <= (radius * radius);
    }


    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
    {
        var result = new List<Vector2I>();
        var tileAreaF = tileArea.ToRect2F();
        var areaCenter = tileAreaF.GetCenter();
        var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;

        for (int x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
        {
            for (int y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
            {
                var tilePos = new Vector2I(x, y);

                if (!IsTileInsideCircle(areaCenter, tilePos, radius + radiusMod) || !filterFn(tilePos)) continue;

                result.Add(tilePos);
            }
        }

        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePos) => GetTileCustomData(tilePos, Constants.IS_BUILDABLE).Item2);
    }

    private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePos) => GetTileCustomData(tilePos, Constants.IS_WOOD).Item2);

    }

    private List<TileMapLayer> GetAllTileMapLayer(Node2D rootNode)
    {
        var result = new List<TileMapLayer>();

        var childrens = rootNode.GetChildren();

        childrens.Reverse();

        foreach (var item in childrens)
        {
            if (item is not Node2D childNode) continue;

            result.AddRange(GetAllTileMapLayer(childNode));
        }

        if (rootNode is TileMapLayer layer)
        {
            result.Add(layer);
        }

        return result;
    }


    private void MapTileMapElevationLayer()
    {
        foreach (var item in allTilemapLayer)
        {
            ElevationLayer elevationLayer;
            Node startNode = item;
            do
            {
                var parent = startNode.GetParent();
                elevationLayer = parent as ElevationLayer;
                startNode = parent;
            } while (elevationLayer is null && startNode is not null);

            tileMapElevation[item] = elevationLayer;
        }
    }

    private void OnBuildingPlaced(BulidingComponent buildingComponent)
    {
        UpdateValidBuildArea(buildingComponent);
        UpdateCollectResourceArea(buildingComponent, _initialLoadComplete);
    }

    private void OnBuildingDestroyed(BulidingComponent buildingComponent)
    {
        RecalculateBuildArea(buildingComponent);
    }
}
