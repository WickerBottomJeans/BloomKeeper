using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class MatchGroupResolveResult
    {
        public MatchGroup SourceMatchGroup { get; }
        public IReadOnlyList<(Vector2Int Position, TileImpactResult Outcome)> Impacts { get; }
        public IReadOnlyList<Vector2Int> TriggeredSkillPositions { get; }

        public MatchGroupResolveResult(MatchGroup sourceMatchGroup, IReadOnlyList<(Vector2Int Position, TileImpactResult Outcome)> impacts, IReadOnlyList<Vector2Int> triggeredSkillPositions)
        {
            SourceMatchGroup = sourceMatchGroup;
            Impacts = impacts;
            TriggeredSkillPositions = triggeredSkillPositions;
        }

        public bool IsTriggeredSkillPosition(Vector2Int position)
        {
            foreach (Vector2Int triggeredPosition in TriggeredSkillPositions)
            {
                if (triggeredPosition == position) return true;
            }

            return false;
        }
    }
}
