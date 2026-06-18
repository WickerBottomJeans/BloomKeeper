using UnityEngine;

namespace DefaultNamespace.UI
{
    public class ComboData
    {
        public PetalType TargetPetalType;
        public SpecialSkillType ComboSkillType;

        public ComboData(PetalType targetPetalType, SpecialSkillType comboSkillType)
        {
            TargetPetalType = targetPetalType;
            ComboSkillType = comboSkillType;
        }
    }
    
    public struct SkillActivation
    {
        public Vector2Int Position;
        public SpecialSkillType SkillType;
        public ComboData Combo;
        /// <summary>
        /// Petal that get this skill executed
        /// </summary>
        public Petal CauserPetal;

        //TODO: why do we still have a dedicated SkillType field when this exist???
        public Petal SelfPetal;

        public SkillActivation(Vector2Int position, SpecialSkillType skillType, Petal selfPetal,
            Petal causerPetal = null, ComboData combo = null)
        {
            Position = position;
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
        public System.Collections.Generic.IReadOnlyList<PetalChange> PetalChanges { get; }

        public SkillUseResult(MatchGroup matchGroup,
            System.Collections.Generic.IReadOnlyList<PetalChange> petalChanges = null)
        {
            MatchGroup = matchGroup;
            PetalChanges = petalChanges ?? System.Array.Empty<PetalChange>();
        }
    }
}
