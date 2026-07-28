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

        private static bool TrySwapAndDetect(Tile[,] grid, Vector2Int tilePositionA, Vector2Int tilePositionB)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            if (tilePositionB.x >= cols || tilePositionB.y >= rows) return false;

            Tile tileA = grid[tilePositionA.x, tilePositionA.y];
            Tile tileB = grid[tilePositionB.x, tilePositionB.y];

            if (!PetalSwapper.Validate(tilePositionA, tilePositionB, grid)) return false;
            if (SkillDetector.HasActivationOnSwap(tileA.Petal.Skill, tileB.Petal.Skill)) return true;
            if (!tileA.IsMatchable() || !tileB.IsMatchable()) return false;

            PetalSwapper.ExecuteSwapPetal(tilePositionA, tilePositionB, grid);

            bool hasMatch = MatchDetector.WouldCompleteMatch(grid, tilePositionA.x, tilePositionA.y)
                            || MatchDetector.WouldCompleteMatch(grid, tilePositionB.x, tilePositionB.y);

            PetalSwapper.ExecuteSwapPetal(tilePositionA, tilePositionB, grid);

            return hasMatch;
        }
    }    
}
