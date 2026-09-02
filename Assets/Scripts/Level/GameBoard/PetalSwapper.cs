using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Swap tile petals in Tile[,]
    /// </summary>
    public static class PetalSwapper
    {
        public static bool Validate(Vector2Int tilePositionA, Vector2Int tilePositionB, Tile[,] grid)
        {
            Tile tileA = grid[tilePositionA.x, tilePositionA.y];
            Tile tileB = grid[tilePositionB.x, tilePositionB.y];

            return tileA != null && tileB != null && tileA.CanSwapPetal() && tileB.CanSwapPetal();
        }

        public static void ExecuteSwapPetal(Vector2Int tilePositionA, Vector2Int tilePositionB, Tile[,] grid)
        {
            Petal petalA = grid[tilePositionA.x, tilePositionA.y].Petal;
            Petal petalB = grid[tilePositionB.x, tilePositionB.y].Petal;
            grid[tilePositionA.x, tilePositionA.y].SetPetal(petalB);
            grid[tilePositionB.x, tilePositionB.y].SetPetal(petalA);
        }
    }
}
