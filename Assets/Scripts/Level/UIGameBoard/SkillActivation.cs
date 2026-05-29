using UnityEngine;

namespace DefaultNamespace.UI
{
    public struct SkillActivation
    {
        public Vector2Int Position;
        public SpecialSkillType SkillType;

        /// <summary>
        /// Petal that get this skill executed
        /// </summary>
        public Petal CauserPetal;

        public Petal SelfPetal;

        public SkillActivation(Vector2Int position, SpecialSkillType skillType, Petal selfPetal,
            Petal causerPetal = null)
        {
            Position = position;
            SkillType = skillType;
            CauserPetal = causerPetal;
            SelfPetal = selfPetal;
        }
    }
}