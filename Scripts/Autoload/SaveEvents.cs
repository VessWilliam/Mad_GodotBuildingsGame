using Game.Extentions;
using Godot;
using Newtonsoft.Json;

namespace Game.Autoload;

public partial class SaveEvents : Node
{
    public static SaveEvents Instance {get; private set;}

    public override void _Notification(int what)
    {
        if(what == NotificationSceneInstantiated)
            Instance = this;
    }


    public override void _Ready()
    {
        var saveData = new SaveData();
        saveData.SaveLevelCompletion("random_id", true);
        var dataString = JsonConvert.SerializeObject(saveData);

        GD.Print(dataString);
    }
}
