using Game.Grids;
using Godot;

namespace Game.Buildings.Contexts;

public class BuildingPlacementContext : BuildingsBaseContext
{
    public BuildingPlacementContext(Node2D ysortRoot, PackedScene cursorScene,
           GridManager gridManager) : base(gridManager)
    {
        YsortRoot = ysortRoot;
        CursorScene = cursorScene;
    }

    public Node2D YsortRoot { get; set; }

    public PackedScene CursorScene { get; set; }
}