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

        if (!tileA.IsMatchable() || !tileB.IsMatchable()) return false;

        Petal temp = tileA.Petal;
        tileA.Petal = tileB.Petal;
        tileB.Petal = temp;

        bool hasMatch = HasMatchAround(grid, cellA, cols, rows) || HasMatchAround(grid, cellB, cols, rows);

        tileB.Petal = tileA.Petal;
        tileA.Petal = temp;

        return hasMatch;
    }

    private static bool HasMatchAround(Tile[,] grid, Vector2Int cell, int cols, int rows)
    {
        int range = MatchDetector.GetMatchCheckRange();
        return HasRunInLine(grid, cell, cols, rows, horizontal: true, range)
            || HasRunInLine(grid, cell, cols, rows, horizontal: false, range);
    }

    private static bool HasRunInLine(Tile[,] grid, Vector2Int cell, int cols, int rows, bool horizontal, int range)
    {
        int start = horizontal ? Mathf.Max(0, cell.x - range) : Mathf.Max(0, cell.y - range);
        int end   = horizontal ? Mathf.Min(cols - 1, cell.x + range) : Mathf.Min(rows - 1, cell.y + range);

        int count = 0;
        PetalType? lastType = null;

        for (int i = start; i <= end; i++)
        {
            int x = horizontal ? i : cell.x;
            int y = horizontal ? cell.y : i;

            Tile tile = grid[x, y];
            if (tile.IsMatchable() && tile.Petal.PetalType == lastType)
            {
                count++;
                if (count >= MatchDetector.MinRunLength) return true;
            }
            else
            {
                count = tile.IsMatchable() ? 1 : 0;
                lastType = tile.IsMatchable() ? tile.Petal.PetalType : (PetalType?)null;
            }
        }

        return false;
    }
}
}