using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class ComboData
    {
        public PetalType TargetPetalType;
        //TODO: It has such a specific name cuz that is the only use case for now. I bet later it would be renamed and used as something more generic
        public SpecialSkillType SunburstComboType;
        public Vector2Int SourceA;
        public Vector2Int SourceB;

        public ComboData(
            PetalType targetPetalType,
            SpecialSkillType sunburstComboType,
            Vector2Int sourceA,
            Vector2Int sourceB)
        {
            TargetPetalType = targetPetalType;
            SunburstComboType = sunburstComboType;
            SourceA = sourceA;
            SourceB = sourceB;
        }
    }
    
    public struct SkillActivation
    {
        public Vector2Int Position;
        public Vector2 EffectOrigin;
        public SpecialSkillType SkillType;
        public ComboData Combo;
        /// <summary>
        /// Petal that get this skill executed
        /// </summary>
        public Petal CauserPetal;

        //TODO: why do we still have a dedicated SkillType field when this exist???
        public Petal SelfPetal;

        public SkillActivation(Vector2Int position, SpecialSkillType skillType, Petal selfPetal,
            Petal causerPetal = null, ComboData combo = null, Vector2? effectOrigin = null)
        {
            Position = position;
            EffectOrigin = effectOrigin ?? position;
            SkillType = skillType;
            CauserPetal = causerPetal;
            SelfPetal = selfPetal;
            Combo = combo;
        }
    }

    public readonly struct PetalChange
    {
        public Vector2Int Position { get; }
        public Petal Before { get; }
        public Petal After { get; }

        public PetalChange(Vector2Int position, Petal before, Petal after)
        {
            Position = position;
            Before = before;
            After = after;
        }
    }

    public sealed class SkillUseResult
    {
        public MatchGroup MatchGroup { get; }
        public SkillRepresentationData Representation { get; }

        public SkillUseResult(
            MatchGroup matchGroup,
            SkillRepresentationData representation = null)
        {
            MatchGroup = matchGroup;
            Representation = representation;
        }
    }

}
