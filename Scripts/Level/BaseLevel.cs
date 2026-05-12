using Game.Manager;
using Game.Gold;
using Godot;
using Game.Camera;

namespace Game.Level;

public partial class BaseLevel : Node
{
    private GridManager gridManager;

    private GoldMine goldMine;

    private GameCamera gameCamera;

    private TileMapLayer terrainTileMapLayer;

    public override void _Ready()
    {
        gridManager = GetNode<GridManager>("GridManager");
        goldMine = GetNode<GoldMine>("%GoldMine");
        gameCamera = GetNode<GameCamera>("GameCamera");
        terrainTileMapLayer = GetNode<TileMapLayer>("%TerrianTileMapLayer");

        gameCamera.SetBoundingRect(terrainTileMapLayer.GetUsedRect());

        gridManager.GridStateUpdate += OnGridStateUpdated;
    }

    private void OnGridStateUpdated()
    {
        var goldMinePos = gridManager.ConvertWorldtoTilePosition(goldMine.GlobalPosition);

        if (gridManager.IsTilePositionBuildable(goldMinePos))
        {
            goldMine.SetActive();
            GD.Print("Win");
        }
    }
}
