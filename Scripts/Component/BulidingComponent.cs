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

        if (!string.IsNullOrEmpty(buildingResourcePath))
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

    public void Destroy()
    {
        GameEvents.EmitBuildingDestroyed(this);
        Owner.QueueFree();
    }

}
