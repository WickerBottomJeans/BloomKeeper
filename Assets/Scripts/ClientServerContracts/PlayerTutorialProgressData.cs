using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class PlayerTutorialProgressData
    {
        [JsonProperty(Required = Required.Always)] public int schemaVersion = TutorialProgressContract.CurrentSchemaVersion;
        [JsonProperty(Required = Required.Always)] public HashSet<string> completedTutorialIds = new HashSet<string>();
    }
}
