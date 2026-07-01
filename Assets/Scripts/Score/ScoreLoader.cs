using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public static class ScoreLoader
    {
        private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, "score_config.json");

        public static ScoreConfigJson Load()
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<ScoreConfigJson>(json);
        }
    }
}
