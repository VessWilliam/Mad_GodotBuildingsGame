using System.Linq;
using Game.Buildings;
using Game.Component;
using Game.Extentions;
using Game.Generals;
using Game.Resources;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int resourceCount);

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

    private Rect2I hoverGridArea = new(Vector2I.Zero, Vector2I.One);

    private BuildingGhost buidingGhost;
    private int currentResourceCount;
    private int currentUsedResourceCount;
    private int statingResourceCount;
    private Vector2 buildingGhostDemensions;

    private BuildingResource toPlaceBuildResource;

    private int AvailableResourceCount =>
         statingResourceCount +
         currentResourceCount -
         currentUsedResourceCount;

    private StateEnum currentState = StateEnum.Normal;

    public override void _Ready()
    {
        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
    }

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
                 IsAbleToBuildAtArea(hoverGridArea))
                    PlaceBuildingAtHovered();

                break;
        }
    }

    public void SetStatingResourceCount(int startcount) => statingResourceCount = startcount;

    public override void _Process(double delta)
    {
        Vector2I mouseGridPos = Vector2I.Zero;

        switch (currentState)
        {
            case StateEnum.Normal:
                mouseGridPos = gridManager.GetMouseGridCellPosition();
                break;
            case StateEnum.PlacingBuilding:
                mouseGridPos = gridManager.GetMouseGridCellPositionWithOffset(buildingGhostDemensions);
                buidingGhost.GlobalPosition = mouseGridPos * 64;
                break;
        }

        var rootCell = hoverGridArea.Position;
        if (rootCell != mouseGridPos)
        {
            hoverGridArea.Position = mouseGridPos;
            UpdateHoveredGridArea();
        }
    }

    private void UpdateGridDisplay()
    {
        gridManager.ClearHighlightArea();

        gridManager.HighlightBuildArea();

        gridManager.HighlightGoblinOccupiedArea();

        buidingGhost.DoHoverAnimation();
        if (!IsAbleToBuildAtArea(hoverGridArea))
        {
            buidingGhost.SetInvalid();
            return;
        }

        gridManager.HighlightExpandBuildArea(hoverGridArea, toPlaceBuildResource.BuildingRadius);
        gridManager.HighlightResourceArea(hoverGridArea, toPlaceBuildResource.ResourceRadius);
        buidingGhost.SetValid();

    }

    private void PlaceBuildingAtHovered()
    {
        var building = toPlaceBuildResource.BuildingScene.Instantiate<Node2D>();

        building.GlobalPosition = hoverGridArea.Position * 64;

        building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayPlaceAnimation();

        ySortRoot.AddChild(building);

        currentUsedResourceCount += toPlaceBuildResource.ResourceCost;

        ChangeState(StateEnum.Normal);

        GD.Print($"Available Resource Count: {AvailableResourceCount}");
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHovered()
    {
        var rootCell = hoverGridArea.Position;

        var building = BulidingComponent.GetValidBuildingComponent(this)
        .FirstOrDefault(b => b.BuildingResource.IsDeletable && b.IsBuildArea(rootCell));

        if (building is null) return;

        currentUsedResourceCount -= building.BuildingResource.ResourceCost;

        building.Destroy();

        GD.Print($"Available Resource Count: {AvailableResourceCount}");
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);

    }

    private void ClearBuildGhost()
    {
        gridManager.ClearHighlightArea();

        if (IsInstanceValid(buidingGhost)) buidingGhost.QueueFree();

        buidingGhost = null;
    }

    private void UpdateHoveredGridArea()
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

    private bool IsAbleToBuildAtArea(Rect2I tileArea)
    {
        var allTileBuildable = gridManager.IsTileAreaBuildable(tileArea);
        return allTileBuildable && AvailableResourceCount >= toPlaceBuildResource.ResourceCost;
    }

    private void OnBuildingResourceSelected(BuildingResource resource)
    {

        ChangeState(StateEnum.PlacingBuilding);
        hoverGridArea.Size = resource.Dimensions;
        var buildiingSprite = resource.SpriteScene.Instantiate<Node2D>();
        buidingGhost.AddSpriteNode(buildiingSprite);
        buidingGhost.SetDemensions(resource.Dimensions);

        buildingGhostDemensions = resource.Dimensions;
        toPlaceBuildResource = resource;
        UpdateGridDisplay();
    }

    private void OnResourceTileUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
        GD.Print($"Resource tile updated: {resourceCount}");
        GD.Print($"Available: {AvailableResourceCount}");
    }

}
