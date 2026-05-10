using Game.Manager;
using Game.Gold;
using Godot;

namespace Game;

public partial class Main : Node
{
    private GridManager gridManager;

    private GoldMine goldMine;

    public override void _Ready()
    {
        gridManager = GetNode<GridManager>("GridManager");
        goldMine = GetNode<GoldMine>("%GoldMine");

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
