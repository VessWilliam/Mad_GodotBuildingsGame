using System.Linq;
using Game.Resources;
using Godot;

namespace Game.Autoload;

public partial class LevelEvents : Node
{
    [Export]
    private LevelResource[] levelResources;

    private static int currentLevelIndex = default;

    private static LevelEvents instance;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            instance = this;
    }

    public static void ChangeLevel(int index)
    {
        if (index >= instance.levelResources.Length || index < 0) return;

        currentLevelIndex = index;

        var levelResource = instance.levelResources[currentLevelIndex];
        instance.GetTree().ChangeSceneToFile(levelResource.LevelScenePath);
    }

    public static void NextLevel() => ChangeLevel(currentLevelIndex + 1);

    public static LevelResource[] GetLevelResources() => instance.levelResources.ToArray();
    
    public static bool IsLastLevel() => currentLevelIndex == instance.levelResources.Length - 1;

}
