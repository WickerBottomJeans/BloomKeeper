using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class MatchGroupResolveResult
    {
        public MatchGroup SourceMatchGroup { get; }
        public IReadOnlyList<TileChange> TileChanges { get; }
        public IReadOnlyList<SkillActivation> SkillActivations { get; }

        public MatchGroupResolveResult(MatchGroup sourceMatchGroup, IReadOnlyList<TileChange> tileChanges, IReadOnlyList<SkillActivation> skillActivations)
        {
            SourceMatchGroup = sourceMatchGroup;
            TileChanges = tileChanges;
            SkillActivations = skillActivations;
        }

        public IEnumerable<Vector2Int> GetSkillTriggerPositions()
        {
            foreach (SkillActivation activation in SkillActivations)
                yield return activation.ParticipantA.Position;
        }

        public bool IsTriggeredSkillPosition(Vector2Int position)
        {
            foreach (SkillActivation activation in SkillActivations)
            {
                if (activation.ParticipantA.Position == position) return true;
            }

            return false;
        }
    }
}
