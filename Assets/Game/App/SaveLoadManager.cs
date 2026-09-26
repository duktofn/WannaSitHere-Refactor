using System.IO;
using UnityEngine;

namespace Game.App.SaveAndLoad
{
    public interface IGameDataStore
    {
        GameData GetGameData();
        void SaveGameData(GameData data);
    }

    public class SaveLoadManager : IGameDataStore
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
                return new GameData
                {
                    IsTutorialCompleted = false,
                    IsFirstTimePlaying = true,
                    currentLevel = 1,
                    currentSoundVolume = 100,
                    currentMusicVolume = 100
                };
            }
            string content = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(content);

            // Older saves predate the audio settings flow. Distinguish missing JSON
            // fields from an intentional persisted value of zero so existing saves
            // start at full volume without overwriting later user choices.
            if (!content.Contains("\"currentSoundVolume\""))
                data.currentSoundVolume = 100;

            if (!content.Contains("\"currentMusicVolume\""))
                data.currentMusicVolume = 100;

            return data;
        }
    }
}
