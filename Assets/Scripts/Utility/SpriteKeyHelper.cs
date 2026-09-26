using DefaultNamespace;

namespace DefaultNamespace.Utility
{
    public class SpriteKeyHelper
    {
        public static string GetPetalSpriteKey(PetalType type, SpecialSkillType skill)
        {
            if (skill == SpecialSkillType.PrismaticBloom)
            {
                return "PrismaticBloom";
            }
            string skillName = skill == SpecialSkillType.None ? "Default" : skill.ToString();
            return $"{type}_{skillName}";
        }
        
        public static string GetObjectiveSpriteKey(ObjectiveType objectiveType)
        {
            return $"Objective_{objectiveType}";
        }
    }
}
