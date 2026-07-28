using System.Collections.Generic;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public static class SkillPresentationImpactQueries
    {
        /// <summary>
        /// Return tile positions where this matchgroup resolution 
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static HashSet<Vector2Int> GetRemovedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var tileChange in resolution.TileChanges)
            {
                if (tileChange.PetalWasRemoved)
                    positions.Add(tileChange.Position);
            }
            return positions;
        }

        public static HashSet<Vector2Int> GetCurrentSkillConsumedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var tileChange in resolution.TileChanges)
            {
                if (tileChange.PetalWasRemoved && !resolution.IsTriggeredSkillPosition(tileChange.Position))
                    positions.Add(tileChange.Position);
            }
            return positions;
        }

        public static HashSet<Vector2Int> GetChangedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var tileChange in resolution.TileChanges)
            {
                if (tileChange.ObstacleLayerChanged)
                    positions.Add(tileChange.Position);
            }
            return positions;
        }

        public static bool WasPetalRemovedAt(MatchGroupResolveResult resolution, Vector2Int position)
        {
            foreach (var tileChange in resolution.TileChanges)
            {
                if (tileChange.Position == position && tileChange.PetalWasRemoved)
                    return true;
            }
            return false;
        }
    }
}
