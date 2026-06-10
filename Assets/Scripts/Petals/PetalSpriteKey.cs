using DefaultNamespace;

namespace Petals
{
    public class PetalSpriteKey
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
    }
}