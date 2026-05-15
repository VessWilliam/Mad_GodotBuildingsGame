using Game.Autoload;
using Game.Component;
using Game.Generals;
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
    private HashSet<Vector2I> occupiedBuild = new();

    private List<TileMapLayer> allTilemapLayer = new();


    public override void _Ready()
    {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;

        allTilemapLayer = GetAllTileMapLayer(terrainTilemapLayer);
    }

    public void ClearHighlightArea() => highlightTilemapLayer.Clear();


    public bool TileHasCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var item in allTilemapLayer)
        {
            var customData = item.GetCellTileData(tilePosition);

            if (customData is null || (bool)customData.GetCustomData(Constants.IS_IGNORE)) continue;

            return (bool)customData.GetCustomData(dataName);
        }

        return false;
    }

    public bool IsTilePositionBuildable(Vector2I tilePosition) => validBuildableArea.Contains(tilePosition);



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
        occupiedBuild.UnionWith(buildingComponent.GetOccupiedCellList());

        var rootCell = buildingComponent.GetGridCellPosition();

        int radius = buildingComponent.BuildingResource.BuildingRadius;
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        var validTiles = GetValidTilesInRadius(tileArea, radius);

        validBuildableArea.UnionWith(validTiles);

        validBuildableArea.ExceptWith(occupiedBuild);

        if (emitSignal)
        {
            EmitSignal(SignalName.GridStateUpdate);
        }
    }

    private void RecalculateBuildArea(BulidingComponent excludeComponent)
    {
        occupiedBuild.Clear();
        validBuildableArea.Clear();
        collectedResourceTiles.Clear();

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


    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
    {
        var result = new List<Vector2I>();

        for (int x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
        {
            for (int y = tileArea.Position.Y - radius; y <= tileArea.End.Y + radius; y++)
            {
                var tilePos = new Vector2I(x, y);

                if (!filterFn(tilePos)) continue;

                result.Add(tilePos);
            }
        }

        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePos) =>
        {
            return TileHasCustomData(tilePos, Constants.IS_BUILDABLE);
        });
    }

    private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePos) =>
      {
          return TileHasCustomData(tilePos, Constants.IS_WOOD);
      });
    }

    private List<TileMapLayer> GetAllTileMapLayer(TileMapLayer rootTileMapLayer)
    {
        var result = new List<TileMapLayer>();

        var childrens = rootTileMapLayer.GetChildren();

        childrens.Reverse();

        foreach (var item in childrens)
        {
            if (item is not TileMapLayer childLayer) continue;

            result.AddRange(GetAllTileMapLayer(childLayer));
        }

        result.Add(rootTileMapLayer);

        return result;
    }

    private void OnBuildingPlaced(BulidingComponent buildingComponent)
    {
        UpdateValidBuildArea(buildingComponent);
        UpdateCollectResourceArea(buildingComponent);
    }

    private void OnBuildingDestroyed(BulidingComponent buildingComponent)
    {
        RecalculateBuildArea(buildingComponent);
    }
}
