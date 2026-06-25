using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Swap cell petals in BoardCell[,]
    /// </summary>
    public static class PetalSwapper
    {
        public static bool Validate(Vector2Int cellA, Vector2Int cellB, BoardCell[,] grid)
        {
            BoardCell boardCellA = grid[cellA.x, cellA.y];
            BoardCell boardCellB = grid[cellB.x, cellB.y];

            return boardCellA.CanSwapPetal() && boardCellB.CanSwapPetal();
        }

        public static void ExecuteSwapPetal(Vector2Int cellA, Vector2Int cellB, BoardCell[,] grid)
        {
            Petal temp = grid[cellA.x, cellA.y].Petal;
            grid[cellA.x, cellA.y].Petal = grid[cellB.x, cellB.y].Petal;
            grid[cellB.x, cellB.y].Petal = temp;
        }
    }
}
