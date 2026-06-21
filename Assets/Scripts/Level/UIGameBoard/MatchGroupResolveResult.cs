using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class MatchGroupResolveResult
    {
        public MatchGroup SourceMatchGroup { get; }
        public IReadOnlyList<(Vector2Int Position, TileImpactResult Outcome)> Impacts { get; }

        public MatchGroupResolveResult(MatchGroup sourceMatchGroup,
            IReadOnlyList<(Vector2Int Position, TileImpactResult Outcome)> impacts)
        {
            SourceMatchGroup = sourceMatchGroup;
            Impacts = impacts;
        }
    }
}
