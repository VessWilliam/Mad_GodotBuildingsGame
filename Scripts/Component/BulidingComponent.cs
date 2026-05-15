using System.Collections.Generic;
using Game.Autoload;
using Game.Resources;
using Godot;

namespace Game.Component;

public partial class BulidingComponent : Node2D
{
    [Export(PropertyHint.File, "*.tscn")]
    public string buildingResourcePath;

    public BuildingResource BuildingResource;

    public override void _Ready()
    {

        if (BuildingResource is null && !string.IsNullOrEmpty(buildingResourcePath))
        {
            BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
        }

        AddToGroup(nameof(BulidingComponent));
        Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();
    }

    public Vector2I GetGridCellPosition()
    {
        var globalPos = (GlobalPosition / 64).Floor();

        return new((int)globalPos.X, (int)globalPos.Y);
    }

    public List<Vector2I> GetOccupiedCellList()
    {
        var result = new List<Vector2I>();
        var gridPos = GetGridCellPosition();
        var dimension = BuildingResource.Dimensions;

        for (int x = 0; x < gridPos.X + dimension.X; x++) 
        {
            for (int y = 0; y < gridPos.Y + dimension.Y; y++) 
            {
                result.Add(new(x, y));
            }
        }

        return result;  
    }

    public void Destroy()
    {
        GameEvents.EmitBuildingDestroyed(this);
        Owner.QueueFree();
    }

}
