using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class DeadlockDetector
    {
        public static bool HasValidMove(Tile[,] grid)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (TrySwapAndDetect(grid, new Vector2Int(x, y), new Vector2Int(x + 1, y))) return true;
                    if (TrySwapAndDetect(grid, new Vector2Int(x, y), new Vector2Int(x, y + 1))) return true;
                }
            }

            return false;
        }

        private static bool TrySwapAndDetect(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            if (cellB.x >= cols || cellB.y >= rows) return false;

            Tile tileA = grid[cellA.x, cellA.y];
            Tile tileB = grid[cellB.x, cellB.y];

            if (!PetalSwapper.Validate(cellA, cellB, grid)) return false;
            if (SkillDetector.HasActivationOnSwap(tileA.Petal.Skill, tileB.Petal.Skill)) return true;
            if (!tileA.IsMatchable() || !tileB.IsMatchable()) return false;

            PetalSwapper.ExecuteSwapPetal(cellA, cellB, grid);

            bool hasMatch = MatchDetector.WouldCompleteMatch(grid, cellA.x, cellA.y)
                            || MatchDetector.WouldCompleteMatch(grid, cellB.x, cellB.y);

            PetalSwapper.ExecuteSwapPetal(cellA, cellB, grid);

            return hasMatch;
        }
    }    
}
