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

            return tileA.CanSwapPetal() && tileB.CanSwapPetal();
        }

        public static void ExecuteSwapPetal(Vector2Int cellA, Vector2Int cellB, Tile[,] grid)
        {
            Petal temp = grid[cellA.x, cellA.y].Petal;
            grid[cellA.x, cellA.y].Petal = grid[cellB.x, cellB.y].Petal;
            grid[cellB.x, cellB.y].Petal = temp;
        }
    }
}
