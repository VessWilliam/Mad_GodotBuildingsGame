using Game.Component;

namespace Game.Feature.Grids.Services.IServices;

public interface IGridStateService
{
    GridStats Stats { get; }

    void UpdateForPlacement(BuildingComponent component);
    void UpdateForDestruction(BuildingComponent component);
    void UpdateForEnabled(BuildingComponent component);
    void UpdateForDisabled(BuildingComponent component);

    void Recalculate();
}