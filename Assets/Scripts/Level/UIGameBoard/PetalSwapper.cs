using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Swap tile's petal in Tile[,]
    /// </summary>
    public static class  PetalSwapper
    {
        public static bool Validate(Vector2Int cellA, Vector2Int cellB, Tile[,] grid)
        {
            Tile tileA = grid[cellA.x, cellA.y];
            Tile tileB = grid[cellB.x, cellB.y];

            if (tileA is not NormalTile || tileB is not NormalTile) return false;
            if (tileA.Petal == null || tileB.Petal == null) return false;

            return true;
        }

        public static void ExecuteSwapPetal(Vector2Int cellA, Vector2Int cellB, Tile[,] grid)
        {
            Petal temp = grid[cellA.x, cellA.y].Petal;
            grid[cellA.x, cellA.y].Petal = grid[cellB.x, cellB.y].Petal;
            grid[cellB.x, cellB.y].Petal = temp;
        }
    }
}