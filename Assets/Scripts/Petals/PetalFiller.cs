using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Petals
{
    public static class PetalFiller
    {
        public static List<Vector2Int> Fill(Tile[,] grid)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            List<Vector2Int> filled = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            {
                bool foundFillEntryCandidate = false;

                for (int y = rows - 1; y >= 0; y--)
                {
                    Tile tile = grid[x, y];

                    if (!foundFillEntryCandidate)
                    {
                        if (tile == null) continue;
                        foundFillEntryCandidate = true;
                    }

                    if (tile == null || !tile.IsGravityAffected()) break;
                    if (!tile.CanReceiveNewPetal()) continue;

                    tile.SetPetal(PetalFactory.CreateRandom());
                    filled.Add(new Vector2Int(x, y));
                }
            }

            return filled;
        }
    }
}
