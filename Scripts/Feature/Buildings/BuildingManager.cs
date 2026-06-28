using Game.Feature.Buildings.Services.IServices;
using Game.Utils;
using Game.Feature.Grids;
using Game.Resources;
using Game.UI;
using Godot;
using Game.Feature.Buildings.Services;
using Game.Feature.FloatingTexts;
using System.Collections.Generic;
using System.Linq;

namespace Game.Feature.Buildings;

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
    private int statingResourceCount;

    private IBuildingRemove _removeService;
    private IBuildingPlacement _placementService;

    private StateEnum currentState = StateEnum.Normal;



    private readonly Dictionary<int, int> _buildingSpend = new();

    private int CurrentSpend => _buildingSpend.Values.Sum();

    private int AvailableResourceCount =>
        Mathf.Max(0, statingResourceCount + currentResourceCount - CurrentSpend);

    private float _lastGridUpdateTime = 0;

    public override void _Ready()
    {
        _placementService = new BuildingPlacementService(gridManager, ySortRoot, buidingGhostScene);

        _removeService = new BuildingRemoveService(gridManager, this);

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

                if (evt.IsActionPressed(ACTION_LEFT_CLICK))
                {
                    var cost = _placementService.GetPlacementCost();

                    if (!_placementService.IsConfirmPlacement())
                    {
                        FloatingTextManager.ShowMessage("Can't build here");
                        return;
                    }


                    if (AvailableResourceCount < cost)
                    {
                        FloatingTextManager.ShowMessage("Not enough wood");
                        return;
                    }

                    GD.Print("=== RESOURCE CHECK ===");
                    GD.Print($"Available: {AvailableResourceCount}");
                    GD.Print($"Cost: {_placementService.GetPlacementCost()}");


                    GD.Print($"After Build Available: {AvailableResourceCount}");

                    var instanceId = _placementService.ConfrimPlacement();
                    if (instanceId != -1)
                        _buildingSpend[instanceId] = cost;

                    ChangeState(StateEnum.Normal);
                    //EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
                }
                break;
        }
    }

    public void SetStatingResourceCount(int startcount) => statingResourceCount = startcount;

    public override void _Process(double delta)
    {
        if (currentState is StateEnum.Normal) return;

        float currentTime = Time.GetTicksMsec() / 1000.0f;
        if (currentTime - _lastGridUpdateTime < 0.05f)
            return;
        _lastGridUpdateTime = currentTime;

        var mouseGridPos = gridManager.GetMouseGridCellPositionWithDimensionOffset(
            _placementService.GetHoverGridArea().Size);

        _placementService.UpdateMousePosition(mouseGridPos);
        _placementService.UpdateGridDisplay();
    }

    private void RemoveBuildingAtHovered()
    {
        var rootCell = gridManager.GetMouseGridCellPosition();

        if (!_removeService.IsRemove(rootCell, out int refund, out int instanceId)) return;
        _buildingSpend.Remove(instanceId);
        //EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
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
        Callable.From(() =>
        {
            currentResourceCount = resourceCount;
            EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
        }).CallDeferred();
    }
}