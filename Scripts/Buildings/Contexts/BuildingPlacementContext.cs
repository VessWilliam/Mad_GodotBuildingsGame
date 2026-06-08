using Game.Manager;
using Godot;

namespace Game.Buildings.Contexts;

public class BuildingPlacementContext
{
    public GridManager GridManager { get; set; }

    public Node2D YsortRoot { get; set; }

    public PackedScene CursorScene { get; set; }
}