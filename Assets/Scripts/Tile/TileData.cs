using System.Collections.Generic;

namespace DefaultNamespace
{
    // fat DTO - only for JSON deserialization
    public class TileData
    {
        public bool isVoid;
        public TileType type;
        public int webLevel;
        public PetalType petalType;
        public SpecialSkillType skillType =  SpecialSkillType.None;
    }
}
