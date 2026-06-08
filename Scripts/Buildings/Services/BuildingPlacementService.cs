using Game.Buildings.Contexts;
using Game.Buildings.Services.IServices;
using Game.Component;
using Game.Extentions;
using Game.Resources;
using Godot;

namespace Game.Buildings.Services;

public class BuildingPlacementService : IBuildingPlacement
{
    private readonly BuildingPlacementContext _context;

    private BuildingCursor _cursor;

    private BuildingResource _resource;

    private Rect2I _hoverGridArea = new(Vector2I.Zero, Vector2I.One);

    public bool IsPlacement => _resource is not null;

    public BuildingPlacementService(BuildingPlacementContext context) => _context = context;


    public void StartPlacement(BuildingResource resource)
    {
        _resource = resource;
        _hoverGridArea.Size = resource.Dimensions;

        _cursor = _context.CursorScene.Instantiate<BuildingCursor>();
        _context.YsortRoot.AddChild(_cursor);

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
        _context.YsortRoot.AddChild(building);
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
     _resource is not null && _context.GridManager.IsTileAreaBuildable(_hoverGridArea, _resource.IsAttackBuilding());


    private void ClearCursor()
    {
        _cursor?.SafeQueueFree();
        _cursor = null;
        _context.GridManager.ClearHighlightedTiles();
    }

    public void UpdateGridDisplay()
    {
        if (!IsPlacement) return;

        _context.GridManager.ClearHighlightedTiles();

        if (_resource.IsAttackBuilding())
        {
            _context.GridManager.HighlightGoblinOccupiedTiles();
            _context.GridManager.HighlightBuildableTiles(true);
        }
        else
        {
            _context.GridManager.HighlightBuildableTiles();
            _context.GridManager.HighlightGoblinOccupiedTiles();
        }

        _cursor?.DoHoverAnimation();

        if (!IsConfirmPlacement())
        {
            _cursor?.SetInvalid();
            return;
        }

        if (_resource.IsAttackBuilding())
            _context.GridManager.HighlightAttackTiles(_hoverGridArea, _resource.AttackRadius);
        else
            _context.GridManager.HighlightExpandedBuildableTiles(_hoverGridArea, _resource.BuildingRadius);

        _context.GridManager.HighlightResourceTiles(_hoverGridArea, _resource.ResourceRadius);
        _cursor?.SetValid();
    }
}

