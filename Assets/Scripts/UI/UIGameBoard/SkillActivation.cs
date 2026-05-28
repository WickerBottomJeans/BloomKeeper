using UnityEngine;

namespace DefaultNamespace.UI
{
    public struct SkillActivation
    {
        public Vector2Int Position;
        public SpecialSkillType SkillType;

        public SkillActivation(Vector2Int position, SpecialSkillType skillType)
        {
            Position = position;
            SkillType = skillType;
        }
    }
}