using System.Collections.Generic;
using DefaultNamespace;

namespace Utility
{
    /// <summary>
    /// Just make sure that no free match would appear right after init :)
    /// </summary>
    public static class BoardInitializer
    {
        private static readonly System.Random rng = new();

        public static Tile[,] Initialize(LevelData data)
        {
            int cols = data.boardWidth;
            int rows = data.boardHeight;
            Tile[,] grid = new Tile[cols, rows];

            for (int i = 0; i < data.tiles.Count; i++)
            {
                int x = i % cols;
                int y = rows - 1 - (i / cols);
                grid[x, y] = TileFactory.Create(data.tiles[i]);
            }

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    int index = (rows - 1 - y) * cols + x;
                    TileData tileData = index < data.tiles.Count ? data.tiles[index] : new TileData();

                    if (grid[x, y] is InactiveTile) continue;

                    grid[x, y].Petal = tileData.petalType != PetalType.None
                        ? PetalFactory.CreateForTileMap(tileData)
                        : CreatePetalWithConstrained(grid, x, y);
                }
            }

            return grid;
        }

        private static Petal CreatePetalWithConstrained(Tile[,] grid, int x, int y)
        {
            PetalType[] allTypes = PetalFactory.RandomPetalTypes;
            List<PetalType> excluded = new List<PetalType>();

            foreach (PetalType type in allTypes)
            {
                grid[x, y].Petal = PetalFactory.CreatePetal(type, SpecialSkillType.None);
                if (MatchDetector.WouldCompleteMatch(grid, x, y))
                    excluded.Add(type);
                grid[x, y].Petal = null;
            }

            PetalType[] candidates = System.Array.FindAll(allTypes, t => !excluded.Contains(t));
            if (candidates.Length == 0) candidates = allTypes;

            PetalType chosen = candidates[rng.Next(candidates.Length)];
            return PetalFactory.CreatePetal(chosen, SpecialSkillType.None);
        }
    }
}