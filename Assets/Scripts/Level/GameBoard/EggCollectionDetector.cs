using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public readonly struct EggCollectionTarget
    {
        public Vector2Int EggPosition { get; }
        public Vector2Int BasketPosition { get; }

        public EggCollectionTarget(Vector2Int eggPosition, Vector2Int basketPosition)
        {
            EggPosition = eggPosition;
            BasketPosition = basketPosition;
        }
    }

    /// <summary>Finds one basket per adjacent egg without changing the board.</summary>
    public static class EggCollectionDetector
    {
        private static readonly Vector2Int[] NeighborOffsets = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static List<EggCollectionTarget> Detect(Tile[,] grid)
        {
            var collections = new List<EggCollectionTarget>();
            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y]?.Piece is not Egg) continue;
                var eggPosition = new Vector2Int(x, y);
                foreach (Vector2Int offset in NeighborOffsets)
                {
                    Vector2Int basketPosition = eggPosition + offset;
                    if (basketPosition.x < 0 || basketPosition.y < 0 || basketPosition.x >= grid.GetLength(0) || basketPosition.y >= grid.GetLength(1)) continue;
                    if (grid[basketPosition.x, basketPosition.y]?.Feature is not BasketFeature) continue;
                    collections.Add(new EggCollectionTarget(eggPosition, basketPosition));
                    break;
                }
            }
            return collections;
        }
    }
}
