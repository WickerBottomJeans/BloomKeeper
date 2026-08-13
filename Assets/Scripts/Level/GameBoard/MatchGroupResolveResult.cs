using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class MatchGroupResolveResult
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

        public IEnumerable<Vector2Int> GetTriggeredSkillInputPositions()
        {
            foreach (SkillActivation activation in SkillActivations)
            foreach (SkillParticipant input in activation.ConsumedInputs)
                yield return input.Position;
        }

        public bool IsTriggeredSkillInputPosition(Vector2Int position)
        {
            foreach (SkillActivation activation in SkillActivations)
            {
                foreach (SkillParticipant input in activation.ConsumedInputs)
                {
                    if (input.Position == position) return true;
                }
            }

            return false;
        }
    }
}
