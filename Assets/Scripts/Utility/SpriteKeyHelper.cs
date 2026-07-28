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
        
        public static string GetTileOverlayKey(TileType tileType, int obstacleLayerCount)
        {
            if (obstacleLayerCount <= 0)
                return null;

            return tileType switch
            {
                TileType.Web => $"Overlay_Web_{obstacleLayerCount}",
                _ => throw new System.ArgumentOutOfRangeException(nameof(tileType), tileType, "Tile overlay sprite is not configured for this obstacle-bearing tile type.")
            };
        }

        public static string GetObjectiveSpriteKey(ObjectiveType objectiveType)
        {
            return $"Objective_{objectiveType}";
        }
    }
}
