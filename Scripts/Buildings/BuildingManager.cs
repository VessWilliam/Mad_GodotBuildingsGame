using System.Linq;
using Game.Buildings.Contexts;
using Game.Buildings.Services;
using Game.Buildings.Services.IServices;
using Game.Component;
using Game.Generals;
using Game.Manager;
using Game.Resources;
using Game.UI;
using Godot;

namespace Game.Buildings;

public partial class BuildingManager : Node
{
    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int resourceCount);

    [Export] private GridManager gridManager;
    [Export] private Node2D ySortRoot;
    [Export] private GameUI gameUI;
    [Export] private PackedScene buidingGhostScene;

    public readonly StringName ACTION_LEFT_CLICK = Constants.LEFT_CLICK;
    public readonly StringName ACTION_RIGHT_CLICK = Constants.RIGHT_CLICK;
    public readonly StringName ACTION_CANCEL = Constants.CANCEL;

    private IBuildingPlacement _placementService;
    private int currentResourceCount;
    private int currentUsedResourceCount;
    private int statingResourceCount;

    private int AvailableResourceCount =>
        statingResourceCount + currentResourceCount - currentUsedResourceCount;

    private StateEnum currentState = StateEnum.Normal;

    public override void _Ready()
    {
        _placementService = new BuildingPlacementService(new BuildingPlacementContext
        {
            GridManager = gridManager,
            YsortRoot = ySortRoot,
            CursorScene = buidingGhostScene
        });

        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount))
            .CallDeferred();
    }

    public override void _EnterTree()
    {
        gridManager.ResourceTilesUpdated += OnResourceTileUpdated;
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

                if (evt.IsActionPressed(ACTION_LEFT_CLICK) && _placementService.IsConfirmPlacement())
                {
                    currentUsedResourceCount += _placementService.GetPlacementCost();
                    _placementService.ConfrimPlacement();
                    ChangeState(StateEnum.Normal);
                    EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
                }
                break;
        }
    }

    public void SetStatingResourceCount(int startcount) => statingResourceCount = startcount;

    public override void _Process(double delta)
    {
        if (currentState is StateEnum.Normal) return;

        var mouseGridPos = gridManager.GetMouseGridCellPositionWithDimensionOffset(
            _placementService.GetHoverGridArea().Size);

        _placementService.UpdateMousePosition(mouseGridPos);
        _placementService.UpdateGridDisplay();
    }

    private void DestroyBuildingAtHovered()
    {
        var rootCell = gridManager.GetMouseGridCellPosition();

        var building = BuildingComponent.GetValidBuildingComponents(this)
            .FirstOrDefault(b =>
                b.BuildingResource.IsDeletable &&
                b.IsTileInBuildingArea(rootCell));

        if (building is null) return;
        if (!gridManager.CanDestroyBuilding(building)) return;

        currentUsedResourceCount -= building.BuildingResource.ResourceCost;
        gridManager.DestroyBuilding(building);
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ChangeState(StateEnum toState)
    {
        if (currentState == StateEnum.PlacingBuilding)
            _placementService.CancelPlacement();
        currentState = toState;
    }

    private void OnBuildingResourceSelected(BuildingResource resource)
    {
        ChangeState(StateEnum.PlacingBuilding);
        _placementService.StartPlacement(resource);
    }

    private void OnResourceTileUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }
}