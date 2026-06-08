using Game.Buildings;
using Game.Manager;
using Game.Gold;
using Godot;
using Game.Camera;
using Game.UI;
using Game.Resources;

namespace Game.Level;

public partial class BaseLevel : Node
{
    [Export]
    private PackedScene levelCompleteScene;

    [Export]
    private LevelResource levelResource;

    private GridManager gridManager;

    private GoldMine goldMine;

    private GameCamera gameCamera;

    private TileMapLayer terrainTileMapLayer;

    private Node2D baseBuilding;

    private GameUI gameUI;

    private BuildingManager buildingManager;

    public override void _Ready()
    {
        gridManager = GetNode<GridManager>("GridManager");
        goldMine = GetNode<GoldMine>("%GoldMine");
        gameCamera = GetNode<GameCamera>("GameCamera");
        terrainTileMapLayer = GetNode<TileMapLayer>("%TerrianTileMapLayer");
        baseBuilding = GetNode<Node2D>("%Base");
        gameUI = GetNode<GameUI>("%GameUI");
        buildingManager = GetNode<BuildingManager>("%BuildingManager");

        gameCamera.SetBoundingRect(terrainTileMapLayer.GetUsedRect());
        gameCamera.SetCenter(baseBuilding.GlobalPosition);

        buildingManager.SetStatingResourceCount(levelResource.StaringResourcesCount);
        gridManager.GridStateUpdated += OnGridStateUpdated;
    }

    private void OnGridStateUpdated()
    {
        var goldMinePos = gridManager.ConvertWorldPositionToTilePosition(goldMine.GlobalPosition);

        if (gridManager.IsTilePositionInAnyBuildingRadius(goldMinePos))
        {
            var levelCompleteInstance = levelCompleteScene.Instantiate<LevelCompleteScreen>();
            AddChild(levelCompleteInstance);

            goldMine.SetActive();
            gameUI.HideUI();
        }
    }
}
