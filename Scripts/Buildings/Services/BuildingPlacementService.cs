using Game.Buildings.Services.IServices;
using Game.Component;
using Game.Extentions;
using Game.Grids;
using Game.Resources;
using Godot;

namespace Game.Buildings.Services;

public class BuildingPlacementService : IBuildingPlacement
{
    private BuildingCursor _cursor;

    private BuildingResource _resource;

    private Rect2I _hoverGridArea = new(Vector2I.Zero, Vector2I.One);

    public bool IsPlacement => _resource is not null;

    public GridManager _gridManager { get; set; }

    private Node2D _ysortRoot { get; set; }

    private PackedScene _cursorScene { get; set; }

    public BuildingPlacementService(GridManager gridManager, Node2D ysortRoot,
          PackedScene cursorScene)
    {
        _gridManager = gridManager;
        _ysortRoot = ysortRoot;
        _cursorScene = cursorScene;
    }


    public void StartPlacement(BuildingResource resource)
    {
        _resource = resource;
        _hoverGridArea.Size = resource.Dimensions;

        _cursor = _cursorScene.Instantiate<BuildingCursor>();
        _ysortRoot.AddChild(_cursor);

        var sprite = resource.SpriteScene.Instantiate<Node2D>();
        _cursor.AddSpriteNode(sprite);
        _cursor.SetDemensions(resource.Dimensions);
    }

    public void CancelPlacement()
    {
        _resource = null;
        ClearCursor();
    }

    public void ConfrimPlacement()
    {
        if (!IsConfirmPlacement()) return;

        GD.Print("=== PLACE BUILDING ===");
        GD.Print($"Resource: {_resource.DisplayName}");
        GD.Print($"Tile: {_hoverGridArea.Position}");


        var building = _resource.BuildingScene.Instantiate<Node2D>();
        building.GlobalPosition = _hoverGridArea.Position * 64;
        _ysortRoot.AddChild(building);
        building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayPlaceAnimation();

        CancelPlacement();
    }

    public void UpdateMousePosition(Vector2I position)
    {
        if (!IsPlacement) return;

        _hoverGridArea.Position = position;

        if (_cursor.IsValid()) _cursor.GlobalPosition = position * 64;
    }

    public Rect2I GetHoverGridArea() => _hoverGridArea;

    public int GetPlacementCost() => _resource?.ResourceCost ?? 0;


    public bool IsConfirmPlacement() =>
     _resource is not null && _gridManager.IsTileAreaBuildable(_hoverGridArea, _resource.IsAttackBuilding());


    private void ClearCursor()
    {
        _cursor?.SafeQueueFree();
        _cursor = null;
        _gridManager.ClearHighlightedTiles();
    }

    public void UpdateGridDisplay()
    {
        if (!IsPlacement) return;

        _gridManager.ClearHighlightedTiles();

        if (_resource.IsAttackBuilding())
        {
            _gridManager.DisplayEnemyOccupiedTiles();
            _gridManager.DisplayBuildableTiles(true);
        }
        else
        {
            _gridManager.DisplayBuildableTiles(false);
            _gridManager.DisplayEnemyOccupiedTiles();
        }

        _cursor?.DoHoverAnimation();

        if (!IsConfirmPlacement())
        {
            _cursor?.SetInvalid();
            return;
        }

        if (_resource.IsAttackBuilding())
            _gridManager.DisplayAttackTiles(_hoverGridArea, _resource.AttackRadius);
        else
            _gridManager.DisplayExpandTiles(_hoverGridArea, _resource.BuildingRadius);

        _gridManager.DisplayResourceTiles(_hoverGridArea, _resource.ResourceRadius);
        _cursor?.SetValid();
    }
}

