using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class BoardShuffler
    {
        public static List<Vector2Int> Shuffle(Tile[,] grid)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            List<Vector2Int> affected = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[x, y] == null || !grid[x, y].IsMatchable()) continue;
                    grid[x, y].SetPetal(PetalFactory.CreateRandom());
                    affected.Add(new Vector2Int(x, y));
                }
            }

            return affected;
        }
    }
}
