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
    public int ResourceCost { get; private set; } = 0;

    [Export]
    public int BuildingRadius { get; private set; } = 0;

    [Export]
    public int ResourceRadius { get; private set; } = 0;

    [Export]
    public PackedScene BuildingScene { get; private set; } = null!;

    [Export]
    public PackedScene SpriteScene { get; private set; } = null!;
}
