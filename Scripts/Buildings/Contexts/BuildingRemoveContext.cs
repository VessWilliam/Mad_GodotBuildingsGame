using Game.Grids;
using Godot;

namespace Game.Buildings.Contexts;

public class BuildingRemoveContext : BuildingsBaseContext
{
    public BuildingRemoveContext(Node rootScene, GridManager gridManager) : base(gridManager) => RootScene = rootScene;

    public Node RootScene { get; set; }
}





