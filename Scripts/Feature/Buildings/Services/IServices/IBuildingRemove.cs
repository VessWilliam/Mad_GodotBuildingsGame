using Godot;

namespace Game.Feature.Buildings.Services.IServices;

public interface IBuildingRemove
{
    bool IsRemove(Vector2I rootCell, out int refundCost, out int instanceId);
}



