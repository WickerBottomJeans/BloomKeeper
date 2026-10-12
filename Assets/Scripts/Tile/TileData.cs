using Newtonsoft.Json;

namespace DefaultNamespace
{
    // An object describes a tile; a null entry in LevelData.tiles describes a hole.
    [JsonObject(MissingMemberHandling = MissingMemberHandling.Error)]
    public class TileData
    {
        public TileFeatureData feature;
        public PetalType petalType;
        public bool hasEgg;
        public SpecialSkillType skillType =  SpecialSkillType.None;
    }
}
