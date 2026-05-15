using Godot;

namespace Game.Resources;

[GlobalClass]
public partial class BuildingResource : Resource
{
    [Export]
    public string DisplayName { get; private set; }

    [Export]
    public Vector2I Dimensions { get; private set; } = Vector2I.One;

    [Export]
    public int ResourceCost { get; private set; }

    [Export]
    public int BuildingRadius { get; private set; }

    [Export]
    public int ResourceRadius { get; private set; }

    [Export]
    public PackedScene BuildingScene { get; private set; }

    [Export]
    public PackedScene SpriteScene { get; private set; }
}
