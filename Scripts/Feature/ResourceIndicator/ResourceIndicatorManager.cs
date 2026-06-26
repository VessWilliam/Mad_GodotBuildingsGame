using System.Collections.Generic;
using System.Linq;
using Game.Feature.Grids;
using Game.UI;
using Godot;

namespace Game.Feature.Collector;


public partial class ResourceIndicatorManager : Node
{
    [Export]
    private GridManager gridManager;

    [Export]
    private PackedScene indicatorScene;

    private HashSet<Vector2I> indicatedTile = new();

    private Dictionary<Vector2I, ResourceIndicator> tileToResourceIndicator = new();

    public override void _Ready()
    {
        gridManager.ResourceTilesUpdated += OnResourceTileUpdated;
    }

    private void UpdateIndicatorTiles(
        IEnumerable<Vector2I> newTiles,
        IEnumerable<Vector2I> removedTiles)
    {
        foreach (var item in newTiles)
        {

            var indicator = indicatorScene.Instantiate<ResourceIndicator>();

            AddChild(indicator);

            indicator.GlobalPosition = item * 64;

            tileToResourceIndicator[item] = indicator;

        }


        foreach (var item in removedTiles)
        {
            tileToResourceIndicator.TryGetValue(item, out var indicator);

            if (IsInstanceIdValid(indicator.GetInstanceId()))
                indicator.Destroy();

            tileToResourceIndicator.Remove(item);
        }


    }

    private void HandleResourceTilesUpdated()
    {

        GD.Print("Updated");

        var currentResourceTile = gridManager.GetResourceTile();

        var newResourceTile = currentResourceTile.Except(indicatedTile);

        var removedResourceTile = indicatedTile.Except(currentResourceTile);

        indicatedTile = currentResourceTile;

        UpdateIndicatorTiles(newResourceTile, removedResourceTile);
    }


    private void OnResourceTileUpdated(int _) 
    {
        Callable.From(() => HandleResourceTilesUpdated()).CallDeferred();
    }

}
