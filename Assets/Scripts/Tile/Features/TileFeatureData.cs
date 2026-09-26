using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class TileFeatureData
    {
        [JsonProperty(Required = Required.Always)] public TileFeatureType type;
        public WebFeatureData web;
    }
}
