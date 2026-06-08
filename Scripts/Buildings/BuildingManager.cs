using Game.Buildings.Contexts;
using Game.Buildings.Services;
using Game.Buildings.Services.IServices;
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

    private int currentResourceCount;
    private int currentUsedResourceCount;
    private int statingResourceCount;

    private int AvailableResourceCount =>
        statingResourceCount + currentResourceCount - currentUsedResourceCount;

    private IBuildingRemove _removeService;
    private IBuildingPlacement _placementService;

    private StateEnum currentState = StateEnum.Normal;

    public override void _Ready()
    {
        _placementService = new BuildingPlacementService(new BuildingPlacementContext(
            gridManager: gridManager,
            ysortRoot: ySortRoot,
            cursorScene: buidingGhostScene));

        _removeService = new BuildingRemoveService(new BuildingRemoveContext(
            gridManager: gridManager,
            rootScene: this));

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
                    RemoveBuildingAtHovered();
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

    private void RemoveBuildingAtHovered()
    {
        var rootCell = gridManager.GetMouseGridCellPosition();

        if (!_removeService.IsRemove(rootCell, out int refund)) return;

        currentUsedResourceCount -= refund;

        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ChangeState(StateEnum toState)
    {
        if (currentState is StateEnum.PlacingBuilding)
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