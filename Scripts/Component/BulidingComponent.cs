using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Resources;
using Godot;

namespace Game.Component;

public partial class BulidingComponent : Node2D
{
    [Export(PropertyHint.File, "*.tscn")]
    private string buildingResourcePath;

    private HashSet<Vector2I> occupiedTiles = new();

    public BuildingResource BuildingResource { get; private set; }

    public override void _Ready()
    {

        if (BuildingResource is null && !string.IsNullOrEmpty(buildingResourcePath))
        {
            BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
        }

        AddToGroup(nameof(BulidingComponent));
        init();
    }

    public Vector2I GetGridCellPosition()
    {
        Vector2 globalPos = (GlobalPosition / 64).Floor();

        return new((int)globalPos.X, (int)globalPos.Y);
    }

    public HashSet<Vector2I> GetOccupiedCellPosition() => occupiedTiles.ToHashSet();

    public bool IsBuildArea(Vector2I tilePos) => occupiedTiles.Contains(tilePos);

    public void Destroy()
    {
        GameEvents.EmitBuildingDestroyed(this);
        Owner.QueueFree();
    }

    private void init()
    {
        CalculateOccupiedCellPosition();
        GameEvents.EmitBuildingPlaced(this);
    }

    private void CalculateOccupiedCellPosition()
    {
        Vector2I gridPos = GetGridCellPosition();
        Vector2I dimension = BuildingResource.Dimensions;

        for (int x = gridPos.X; x < gridPos.X + dimension.X; x++)
        {
            for (int y = gridPos.Y; y < gridPos.Y + dimension.Y; y++)
                occupiedTiles.Add(new(x, y));
        }
    }
}
