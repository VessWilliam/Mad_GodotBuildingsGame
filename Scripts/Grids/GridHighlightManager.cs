
using System.Collections.Generic;
using Game.Grids.Contexts;
using Game.Grids.Services;
using Godot;

namespace Game.Grids;

public partial class GridHighlightManager : Node
{

   [Export]
   private TileMapLayer highlightTileMapLayer;

   private GridHighlightService _highlightService;

   public void Initialize(GridStats stats)
   {
      var context = new GridHighlightContext(highlightTileMapLayer, stats);
      _highlightService = new GridHighlightService(context);
   }

   public void ClearHighlightTiles() => _highlightService.ClearHighlightTile();

   public void DisplayPlacementHighlight(bool IsAttackBuilding)
   {
      if (!IsAttackBuilding)
      {
         _highlightService.HighlightBuildableTiles(false);
         _highlightService.HighlightEnemyOccupiedTiles();
         return;
      }

      _highlightService.HighlightEnemyOccupiedTiles();
      _highlightService.HighlightBuildableTiles(true);
   }


   public void ShowExpandedBuildableTiles(IEnumerable<Vector2I> expandedTiles) =>
       _highlightService.HighlightExpandedTiles(expandedTiles);

   public void ShowAttackTiles(IEnumerable<Vector2I> attackTiles) =>
       _highlightService.HighlightTiles(attackTiles, new Vector2I(1, 0));

   public void ShowResourceTiles(IEnumerable<Vector2I> resourceTiles) =>
       _highlightService.HighlightTiles(resourceTiles, new Vector2I(1, 0));
}