using System.Collections.Generic;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public static class SkillPresentationImpactQueries
    {
        public static HashSet<Vector2Int> GetRemovedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.RemovedPetal != null)
                    positions.Add(impact.Position);
            }
            return positions;
        }

        public static HashSet<Vector2Int> GetCurrentSkillConsumedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.RemovedPetal != null && !resolution.IsTriggeredSkillPosition(impact.Position))
                    positions.Add(impact.Position);
            }
            return positions;
        }

        public static HashSet<Vector2Int> GetChangedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.TileChanged)
                    positions.Add(impact.Position);
            }
            return positions;
        }

        public static bool WasPetalRemovedAt(MatchGroupResolveResult resolution, Vector2Int position)
        {
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Position == position && impact.Outcome.RemovedPetal != null)
                    return true;
            }
            return false;
        }
    }
}
