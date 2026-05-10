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

            if (customData is null) continue;

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

    public void HighlightExpandBuildArea(Vector2I rootCell, int radius)
    {
        HighlightBuildArea();

        var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();

        var expandedTiles = validTiles.Except(validBuildableArea).Except(occupiedBuild);

        var atlasCoords = new Vector2I(1, 0);

        foreach (var item in expandedTiles)
        {
            highlightTilemapLayer.SetCell(item, 0, atlasCoords);
        }
    }

    public void HighlightResourceArea(Vector2I rootCell, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(rootCell, radius);

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

    private void UpdateCollectResourceArea(BulidingComponent buildingComponent)
    {
        int radius = buildingComponent.BuildingResource.BuildingRadius;

        var rootCell = buildingComponent.GetGridCellPosition();

        var resourceTiles = GetResourceTilesInRadius(rootCell, radius);

        var oldResourceTileCount = collectedResourceTiles.Count;

        collectedResourceTiles.UnionWith(resourceTiles);

        if (oldResourceTileCount != collectedResourceTiles.Count)
        {
            EmitSignal(SignalName.ResourceTileUpdate, collectedResourceTiles.Count);
        }

        EmitSignal(SignalName.GridStateUpdate);
    }


    private void UpdateValidBuildArea(BulidingComponent buildingComponent)
    {
        occupiedBuild.Add(buildingComponent.GetGridCellPosition());

        int radius = buildingComponent.BuildingResource.BuildingRadius;

        var rootCell = buildingComponent.GetGridCellPosition();

        var validTiles = GetValidTilesInRadius(rootCell, radius);

        validBuildableArea.UnionWith(validTiles);

        validBuildableArea.ExceptWith(occupiedBuild);

        EmitSignal(SignalName.GridStateUpdate);
    }

    private void RecalculateBuildArea(BulidingComponent excludeComponent)
    {
        occupiedBuild.Clear();
        validBuildableArea.Clear();
        collectedResourceTiles.Clear();

        var buildingComponenets = GetTree()
        .GetNodesInGroup(nameof(BulidingComponent))
        .Cast<BulidingComponent>().Where(b => !Equals(b, excludeComponent))
        .ToList();

        foreach (var item in buildingComponenets)
        {
            UpdateValidBuildArea(item);
            UpdateCollectResourceArea(item);
        }

        EmitSignal(SignalName.ResourceTileUpdate, collectedResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdate);
    }


    private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filterFn)
    {
        var result = new List<Vector2I>();

        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        {
            for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
            {
                var tilePos = new Vector2I(x, y);

                if (!filterFn(tilePos)) continue;

                result.Add(tilePos);
            }
        }

        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
    {
        return GetTilesInRadius(rootCell, radius, (tilePos) =>
        {
            return TileHasCustomData(tilePos, Constants.IS_BUILDABLE);
        });
    }

    private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius)
    {
        return GetTilesInRadius(rootCell, radius, (tilePos) =>
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
