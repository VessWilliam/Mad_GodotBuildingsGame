using System.Linq;
using Game.Buildings;
using Game.Component;
using Game.Generals;
using Game.Resources;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
    [Export]
    private GridManager gridManager;

    [Export]
    private Node2D ySortRoot;

    [Export]
    private GameUI gameUI;

    [Export]
    private PackedScene buidingGhostScene;

    public readonly StringName ACTION_LEFT_CLICK = Constants.LEFT_CLICK;
    public readonly StringName ACTION_RIGHT_CLICK = Constants.RIGHT_CLICK;
    public readonly StringName ACTION_CANCEL = Constants.CANCEL;

    private Vector2I hoverGridCell;
    private int currentResourceCount;
    private int statingResourceCount = 4;
    private int currentUsedResourceCount;
    private BuildingResource toPlaceBuildResource;
    private BuildingGhost buidingGhost;

    private int AvailableResourceCount => statingResourceCount + currentResourceCount - currentUsedResourceCount;
    private StateEnum currentState = StateEnum.Normal;

    public override void _EnterTree()
    {
        gridManager.ResourceTileUpdate += OnResourceTileUpdated;

        gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
    }

    public override void _UnhandledInput(InputEvent evt)
    {

        switch (currentState)
        {
            case StateEnum.Normal:
                if (evt.IsActionPressed(ACTION_LEFT_CLICK))
                    DestroyBuildingAtHovered();
                break;
            case StateEnum.PlacingBuilding:

                if (evt.IsActionPressed(ACTION_CANCEL))
                {
                    ChangeState(StateEnum.Normal);
                    return;
                }

                if (
                 toPlaceBuildResource is not null &&
                 evt.IsActionPressed(ACTION_LEFT_CLICK) &&
                 IsAbleToBuildAtTile(hoverGridCell))
                    PlaceBuildingAtHovered();

                break;
        }
    }

    public override void _Process(double delta)
    {
        var gridPos = gridManager.GetMouseGridCellPosition();


        if (hoverGridCell != gridPos)
        {
            hoverGridCell = gridPos;
            UpdateHoveredGridCell();
        }

        switch (currentState)
        {
            case StateEnum.Normal:
                break;
            case StateEnum.PlacingBuilding:
                buidingGhost.GlobalPosition = gridPos * 64;
                break;

        }

    }

    private void UpdateGridDisplay()
    {
        gridManager.ClearHighlightArea();

        gridManager.HighlightBuildArea();

        if (!IsAbleToBuildAtTile(hoverGridCell))
        {
            buidingGhost.SetInvalid();
            return;
        }

        gridManager.HighlightExpandBuildArea(hoverGridCell, toPlaceBuildResource.BuildingRadius);
        gridManager.HighlightResourceArea(hoverGridCell, toPlaceBuildResource.ResourceRadius);
        buidingGhost.SetValid();
    }

    private void PlaceBuildingAtHovered()
    {
        var building = toPlaceBuildResource.BuildingScene.Instantiate<Node2D>();

        building.GlobalPosition = hoverGridCell * 64;

        ySortRoot.AddChild(building);

        currentUsedResourceCount += toPlaceBuildResource.ResourceCost;

        ChangeState(StateEnum.Normal);

        //GD.Print($"Available Resource Count: {AvailableResourceCount}");
    }

    private void DestroyBuildingAtHovered()
    {

        var building = GetTree()
        .GetNodesInGroup(nameof(BulidingComponent))
        .Cast<BulidingComponent>()
        .FirstOrDefault(b => Equals(b.GetGridCellPosition(), hoverGridCell));

        if (building is null) return;
        
        currentUsedResourceCount -= building.BuildingResource.ResourceCost;

        building.Destroy();

        GD.Print($"Available Resource Count: {AvailableResourceCount}");

    }

    private void ClearBuildGhost()
    {
        gridManager.ClearHighlightArea();

        if (IsInstanceValid(buidingGhost)) buidingGhost.QueueFree();

        buidingGhost = null;
    }


    private void UpdateHoveredGridCell()
    {
        switch (currentState)
        {
            case StateEnum.Normal:
                break;
            case StateEnum.PlacingBuilding:
                UpdateGridDisplay();
                break;
        }

    }

    private void ChangeState(StateEnum toState)
    {
        switch (currentState)
        {
            case StateEnum.Normal:
                break;
            case StateEnum.PlacingBuilding:
                ClearBuildGhost();
                toPlaceBuildResource = null;
                break;
        }

        currentState = toState;

        switch (currentState)
        {
            case StateEnum.Normal:
                break;
            case StateEnum.PlacingBuilding:
                buidingGhost = buidingGhostScene.Instantiate<BuildingGhost>();
                ySortRoot.AddChild(buidingGhost);
                break;
        }
    }


    private bool IsAbleToBuildAtTile(Vector2I tilePos)
    {
        return gridManager.IsTilePositionBuildable(tilePos) &&
        AvailableResourceCount >= toPlaceBuildResource.ResourceCost;
    }

    private void OnBuildingResourceSelected(BuildingResource resource)
    {

        ChangeState(StateEnum.PlacingBuilding);

        var buildiingSprite = resource.SpriteScene.Instantiate<Node2D>();
        buidingGhost.AddChild(buildiingSprite);

        toPlaceBuildResource = resource;
        UpdateGridDisplay();
    }

    private void OnResourceTileUpdated(int resourceCount) => currentResourceCount = resourceCount;

}
