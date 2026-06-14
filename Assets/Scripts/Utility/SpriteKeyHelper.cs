using DefaultNamespace;

namespace DefaultNamespace.Utility
{
    public class SpriteKeyHelper
    {
        public static string GetPetalSpriteKey(PetalType type, SpecialSkillType skill)
        {
            if (skill == SpecialSkillType.Sunburst)
            {
                return "SunBurst";
            }
            string skillName = skill == SpecialSkillType.None ? "Default" : skill.ToString();
            return $"{type}_{skillName}";
        }
        
        public static string GetTileSpriteKey(TileType type)
        {
            return $"Tile_{type}";
        }
        
        public static string GetWebOverlayKey(int webLevel)
        {
            return $"Overlay_Web_{webLevel}";
        }
    }
}