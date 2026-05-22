using Godot;

namespace Game.Resources;

[GlobalClass]
public partial class LevelResource : Resource
{
    [Export]
    public string Id { get; private set; } = string.Empty;
    
    [Export]
    public int StaringResourcesCount { get; private set; } = 4;

    [Export(PropertyHint.File, "*.tscn")]
    public string LevelScenePath { get; private set; } = string.Empty;
}