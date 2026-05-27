using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public static class MatchResolver
    {
        public static void Resolve(List<MatchGroup> matches, Tile[,] grid)
        {
            foreach (MatchGroup match in matches)
            {
                //TODO: impelemtn the special matches logic
                foreach (Vector2Int cell in match.Tiles)
                {
                    grid[cell.x, cell.y].Petal = null;
                }
            }
        }
    }
}