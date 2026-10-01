using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class CompleteTutorialResponse
    {
        [JsonProperty(Required = Required.Always)] public int schemaVersion = TutorialProgressContract.CurrentSchemaVersion;
        [JsonProperty(Required = Required.Always)] public string tutorialId;
        [JsonProperty(Required = Required.Always)] public PlayerTutorialProgressData playerTutorialProgressData;
    }
}
