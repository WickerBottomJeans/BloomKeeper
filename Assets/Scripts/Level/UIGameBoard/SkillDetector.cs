using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class SkillDetector
    {
        public static List<MatchGroup> DetectOnSwap(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            var results = new List<MatchGroup>();

            TryDetectSwapTriggered(grid, cellA, cellB, results);
            TryDetectSwapTriggered(grid, cellB, cellA, results);

            return results;
        }

        private static void TryDetectSwapTriggered(Tile[,] grid, Vector2Int cell, Vector2Int otherCell, List<MatchGroup> results)
        {
            Petal petal = grid[cell.x, cell.y].Petal;
            if (petal == null) return;

            switch (petal.Skill)
            {
                case SpecialSkillType.Sunburst:
                    Petal causerPetal = grid[otherCell.x, otherCell.y].Petal != null
                        ? new Petal(grid[otherCell.x, otherCell.y].Petal)
                        : null;
                    results.Add(new MatchGroup(new List<Vector2Int> { cell }, MatchShape.None, causerPetal));
                    break;

            }
        }
    }
}