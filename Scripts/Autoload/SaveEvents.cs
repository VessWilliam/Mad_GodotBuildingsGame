using System;
using Game.Extentions;
using Game.Resources;
using Godot;
using Newtonsoft.Json;

namespace Game.Autoload;

public partial class SaveEvents : Node
{
    private static readonly string SAVE_PATH = "user://save.json";

    private static SaveData saveData = new();

    public static SaveEvents Instance { get; private set; }

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Instance = this;

        ReadSaveData();
    }

    public static bool IsLevelCompleted(string levelid)
    {

        saveData.LevelCompletedData.TryGetValue(levelid.ToString(), out var data);

        return data?.IsCompleted is true;
    }

    public static void SaveLevelCompletion(LevelResource resouce)
    {
        saveData.SaveLevelCompletion(resouce.Id, true);
        WriteSaveData();
    }

    private static void WriteSaveData()
    {
        var dataString = JsonConvert.SerializeObject(saveData);
        using var savefile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write);
        savefile.StoreLine(dataString);
    }

    private static void ReadSaveData()
    {
        try
        {
            if (!FileAccess.FileExists(SAVE_PATH)) return;

            using var savefile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
            var dataString = savefile.GetLine();

            saveData = JsonConvert.DeserializeObject<SaveData>(dataString);
        }
        catch (Exception)
        {
            GD.PushWarning("save file is corrupted !");
        }
    }
}
