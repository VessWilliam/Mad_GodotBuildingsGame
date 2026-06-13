using Game.Component;
using Game.Grids;
using Game.Grids.Services;
using Game.Grids.Services.IServices;

public class GridStateServices : IGridStateService
{
    
    public GridStats Stats { get; } = new();
     
    private IGridTile _tileServices;
    private readonly GridCache _cache;
    
     
    public GridStateServices(IGridTile tileServices)
    {
        _tileServices = tileServices;
        _cache = new GridCache(_tileServices);
    }
    

    public void Recalculate()
    {   
        throw new System.NotImplementedException();
    }

    public void UpdateForDestruction(BuildingComponent component)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateForDisabled(BuildingComponent component)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateForEnabled(BuildingComponent component)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateForPlacement(BuildingComponent component)
    {
        throw new System.NotImplementedException();
    }
}