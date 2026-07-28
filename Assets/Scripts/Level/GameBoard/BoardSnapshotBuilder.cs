using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public static class BoardSnapshotBuilder
    {
        public static List<TileState> Capture(Tile[,] grid)
        {
            var snapshot = new List<TileState>(grid.Length);
            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                snapshot.Add(CaptureTile(grid, new Vector2Int(x, y)));
            return snapshot;
        }

        public static TileState CaptureTile(Tile[,] grid, Vector2Int position)
        {
            Tile tile = grid[position.x, position.y];
            TileType? tileType = tile?.TileType;
            PetalType? petalType = tile?.Petal?.PetalType;
            SpecialSkillType skillType = tile?.Petal?.Skill ?? SpecialSkillType.None;
            int obstacleLayerCount = tile?.ObstacleLayerCount ?? 0;
            return new TileState(position, tile == null, tileType, petalType, skillType, obstacleLayerCount, tile?.CanClearPetal() ?? false);
        }
    }
}
