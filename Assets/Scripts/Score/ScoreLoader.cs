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
            // TODO: Score config is still local, which isn't ideal. Load it online when there's time.
            string json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<ScoreConfigJson>(json);
        }
    }
}
