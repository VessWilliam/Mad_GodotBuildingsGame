using Godot;

namespace Game.Buildings.Services.IServices;

public interface IBuildingRemove
{
    bool IsRemove(Vector2I rootCell, out int refundCost);
}



