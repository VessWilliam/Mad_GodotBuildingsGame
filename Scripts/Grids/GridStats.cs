using Game.Component;
using Godot;
using System.Collections.Generic;

namespace Game.Grids;

public class GridStats
{
    public HashSet<Vector2I> BuildableTiles { get; } = new();    

    public HashSet<Vector2I> AttackBuildableTiles { get; } = new();

    public HashSet<Vector2I> AllRadiusTiles { get; } = new();

    public HashSet<Vector2I> ResourceTiles { get; } = new();

    public HashSet<Vector2I> OccupiedTiles { get; } = new();

    public HashSet<Vector2I> AttackTiles { get; } = new();

    public HashSet<Vector2I> EnemyOccupiedTiles { get; } = new();

    public Dictionary<BuildingComponent, HashSet<Vector2I>> BuildingRadiusTiles { get; } = new();

    public List<BuildingComponent> PlacementOrder { get; } = new();

    public List<BuildingComponent> OwnerBuildings { get; } = new();
    
    public HashSet<Vector2I> GetBuildableTileSet(bool isAttackTiles = false) =>
        isAttackTiles ? AttackBuildableTiles : BuildableTiles;

    public void Clear()
    {
        BuildableTiles.Clear();
        AttackBuildableTiles.Clear();
        AllRadiusTiles.Clear();
        ResourceTiles.Clear();
        OccupiedTiles.Clear();
        AttackTiles.Clear();
        EnemyOccupiedTiles.Clear();
        BuildingRadiusTiles.Clear();
    }
}