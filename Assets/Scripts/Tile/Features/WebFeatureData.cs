using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class WebFeatureData
    {
        [JsonProperty(Required = Required.Always)] public int layers;
    }
}
