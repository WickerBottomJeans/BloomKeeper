using System;
using System.Collections.Generic;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class ClearSpiderWebObjective : IObjective, IObjectiveTileTargetProvider
    {
        private int spiderWebCount;

        public ClearSpiderWebObjective(ObjectiveJson json)
        {
            spiderWebCount = json.spiderWebsToClear;
        }

        public ObjectiveType ObjectiveType { get; } = ObjectiveType.ClearSpiderWeb;
        public bool CheckObjective() => spiderWebCount <= 0;

        public void Apply(IReadOnlyList<TileChange> changes)
        {
            int clearedWebCount = 0;
            foreach (TileChange change in changes)
            {
                if (change.Before.TileType == TileType.Web && change.ObstacleWasCleared)
                    clearedWebCount++;
            }

            spiderWebCount = Math.Max(0, spiderWebCount - clearedWebCount);
        }

        public List<ObjectiveViewData> GetViewData()
        {
            return new List<ObjectiveViewData>
            {
                new ObjectiveViewData
                {
                    spriteKey = SpriteKeyHelper.GetObjectiveSpriteKey(ObjectiveType),
                    objectiveText = spiderWebCount.ToString(),
                    remainingAmount = spiderWebCount
                }
            };
        }

        public IReadOnlyList<ObjectiveTileTargetGroup> GetTargetGroups(IReadOnlyList<TileState> boardSnapshot)
        {
            var obstaclePositions = new List<Vector2Int>();
            if (CheckObjective()) return Array.Empty<ObjectiveTileTargetGroup>();

            foreach (TileState tile in boardSnapshot)
            {
                if (tile.TileType == TileType.Web && tile.ObstacleLayerCount > 0)
                    obstaclePositions.Add(tile.Position);
            }

            if (obstaclePositions.Count == 0) return Array.Empty<ObjectiveTileTargetGroup>();
            return new[] { new ObjectiveTileTargetGroup(ObjectiveType, obstaclePositions) };
        }
    }
}
