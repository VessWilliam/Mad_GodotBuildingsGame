using Game.Manager;

namespace Game.Buildings.Contexts;

public class BuildingsBaseContext
{   
    protected BuildingsBaseContext(GridManager gridManager) => GridManager = gridManager;

    public GridManager GridManager { get; set; }
}