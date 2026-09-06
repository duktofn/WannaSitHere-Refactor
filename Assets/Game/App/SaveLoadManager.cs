using System.IO;
using UnityEngine;

namespace Game.App.SaveAndLoad
{
    public class SaveLoadManager
    {
        public void SaveGameData(GameData data)
        {
            string content = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "data.json"), content);
        }

        public GameData GetGameData()
        {
            string path = Path.Combine(Application.persistentDataPath, "data.json");
            if (!File.Exists(path))
            {
                return new GameData { currentLevel = 1 };
            }
            string content = File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(content); 
        }
    }
}