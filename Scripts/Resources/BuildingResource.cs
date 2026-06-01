using Godot;

namespace Game.Resources;

[GlobalClass]
public partial class BuildingResource : Resource
{
    [Export]
    public string DisplayName { get; private set; } = string.Empty;

    [Export]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public bool IsDeletable { get; private set; } = true;

    [Export]
    public Vector2I Dimensions { get; private set; } = Vector2I.One;

    [Export]
    public int ResourceCost { get; private set; } = default;

    [Export]
    public int BuildingRadius { get; private set; } = default;

    [Export]
    public int ResourceRadius { get; private set; } = default;

    [Export]
    public int DangerRadius { get; private set; } = default;

    [Export]
    public int AttackRadius { get; private set; } = default;

    [Export]
    public PackedScene BuildingScene { get; private set; } = default!;

    [Export]
    public PackedScene SpriteScene { get; private set; } = default!;

    public bool isAttackTile => AttackRadius > 0;
}
