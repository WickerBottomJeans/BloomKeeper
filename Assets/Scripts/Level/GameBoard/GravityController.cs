using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class GravityController
    {
        public static List<(Vector2Int from, Vector2Int to)> Apply(Tile[,] grid)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            List<(Vector2Int, Vector2Int)> moves = new List<(Vector2Int, Vector2Int)>();

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[x, y] == null || !grid[x, y].CanReceiveNewPetal()) continue;

                    for (int above = y + 1; above < rows; above++)
                    {
                        if (grid[x, above] == null || !grid[x, above].IsGravityAffected()) break;
                        if (grid[x, above].Petal == null) continue;

                        Petal fallingPetal = grid[x, above].Petal;
                        grid[x, y].SetPetal(fallingPetal);
                        grid[x, above].RemovePetal();
                        moves.Add((new Vector2Int(x, above), new Vector2Int(x, y)));
                        break;
                    }
                }
            }

            return moves;
        }
    }
}
