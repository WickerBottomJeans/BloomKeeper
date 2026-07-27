using System;
using System.Collections.Generic;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class ClearSpiderWebObjective : IObjective, IGameplayEventHandler, IObjectiveBoardCellTargetProvider
    {
        private int spiderWebCount;

        public ClearSpiderWebObjective(ObjectiveJson json)
        {
            spiderWebCount = json.spiderWebsToClear;
        }

        public ObjectiveType ObjectiveType { get; } = ObjectiveType.ClearSpiderWeb;
        public bool CheckObjective() => spiderWebCount <= 0;
        public Type HandledEventType => typeof(SpiderWebClearedEvent);

        public void Handle(IGameplayEvent e)
        {
            SpiderWebClearedEvent cleared = (SpiderWebClearedEvent)e;
            spiderWebCount = Math.Max(0, spiderWebCount - cleared.CleanedTileCount);
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

        public IReadOnlyList<ObjectiveBoardCellTargetGroup> GetTargetGroups(BoardCell[,] grid)
        {
            var obstaclePositions = new List<Vector2Int>();
            if (CheckObjective()) return Array.Empty<ObjectiveBoardCellTargetGroup>();

            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                BoardCell cell = grid[x, y];
                if (cell.Tile is WebTile webTile && webTile.HasActiveWeb)
                    obstaclePositions.Add(new Vector2Int(x, y));
            }

            if (obstaclePositions.Count == 0) return Array.Empty<ObjectiveBoardCellTargetGroup>();
            return new[] { new ObjectiveBoardCellTargetGroup(ObjectiveType, obstaclePositions) };
        }
    }
}
