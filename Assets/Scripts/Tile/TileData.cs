using System.Collections.Generic;

using Newtonsoft.Json;

namespace DefaultNamespace
{
    // fat DTO - only for JSON deserialization
    public class TileData
    {
        public bool isVoid;
        [JsonProperty(Required = Required.Always)] public bool isPlayable;
        public TileFeatureData feature;
        public PetalType petalType;
        public SpecialSkillType skillType =  SpecialSkillType.None;
    }
}
