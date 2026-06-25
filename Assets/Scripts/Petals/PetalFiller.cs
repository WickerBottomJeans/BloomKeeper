using System.Collections.Generic;
using DefaultNamespace.UI;
using UnityEngine;

namespace Petals
{
    public static class PetalFiller
    {
        public static List<Vector2Int> Fill(BoardCell[,] grid)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            List<Vector2Int> filled = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            {
                for (int y = rows - 1; y >= 0; y--)
                {
                    if (!grid[x, y].IsGravityAffected()) break;
                    if (!grid[x, y].CanReceiveNewPetal()) continue;

                    grid[x, y].Petal = PetalFactory.CreateRandom();
                    filled.Add(new Vector2Int(x, y));
                }
            }

            return filled;
        }
    }
}
