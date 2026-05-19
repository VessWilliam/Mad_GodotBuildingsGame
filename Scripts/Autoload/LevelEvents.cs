using Godot;

namespace Game.Autoload;

public partial class LevelEvents : Node
{
    [Export]
    private PackedScene[] levelSecens;

    private int currentLevelIndex = default;

    public static LevelEvents Instance { get; private set; }

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Instance = this;
    }

    public void ChangeLevel(int index)
    {
        if (index >= levelSecens.Length || index < 0) return;

        currentLevelIndex = index;

        var levelScene = levelSecens[currentLevelIndex];
        GetTree().ChangeSceneToPacked(levelScene);

    }

    public void NextLevel() => ChangeLevel(currentLevelIndex + 1);
}
