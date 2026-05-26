using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public static class LevelLoader
    {
        private static string BasePath => Path.Combine(Application.streamingAssetsPath, "levels");

        public static LevelData Load(int levelId)
        {
            string path = Path.Combine(BasePath, $"level_{levelId}.json");
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<LevelData>(json);
        }
        
        public static LevelMetaCollection LoadMetas()
        {
            string path = Path.Combine(BasePath, "level_meta.json");
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<LevelMetaCollection>(json);
        }
    }
}