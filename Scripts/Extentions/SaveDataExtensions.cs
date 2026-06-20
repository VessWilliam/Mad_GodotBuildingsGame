
using Game.Data;
using System.Collections.Generic;

namespace Game.Extentions;

public class SaveData
{
    public Dictionary<string, LevelCompleteData> LevelCompletedData {get; private set;} = new();

    public void SaveLevelCompletion(string id, bool completed)
    {
        if (!LevelCompletedData.ContainsKey(id))
            LevelCompletedData[id] = new();

        LevelCompletedData[id].IsCompleted = completed;
    }
}