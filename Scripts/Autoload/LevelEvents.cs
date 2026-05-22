using System.Linq;
using Game.Resources;
using Godot;

namespace Game.Autoload;

public partial class LevelEvents : Node
{
    [Export]
    private LevelResource[] levelResources;

    private int currentLevelIndex = default;

    public static LevelEvents Instance { get; private set; }

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Instance = this;
    }

    public void ChangeLevel(int index)
    {
        if (index >= levelResources.Length || index < 0) return;

        currentLevelIndex = index;

        var levelResource = levelResources[currentLevelIndex];
        GetTree().ChangeSceneToFile(levelResource.LevelScenePath);
    }

    public void NextLevel() => ChangeLevel(currentLevelIndex + 1);

    public static LevelResource[] GetLevelResources() => Instance.levelResources.ToArray();
}
