using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;

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
                TileData tileData = data.tiles[i];
                grid[x, y] = tileData.isVoid ? null : TileFactory.Create(tileData);
            }

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    int index = (rows - 1 - y) * cols + x;
                    TileData tileData = index < data.tiles.Count ? data.tiles[index] : new TileData();
                    Tile tile = grid[x, y];

                    if (tile == null) continue;
                    if (!tile.CanReceiveNewPetal())
                    {
                        if (tileData.petalType != PetalType.None || tileData.skillType != SpecialSkillType.None)
                            throw new System.InvalidOperationException($"Tile at ({x}, {y}) cannot contain configured petal data.");
                        continue;
                    }

                    tile.SetPetal(tileData.petalType != PetalType.None
                        ? PetalFactory.CreateForTileMap(tileData)
                        : CreatePetalWithConstrained(grid, x, y));
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
                grid[x, y].SetPetal(PetalFactory.CreatePetal(type, SpecialSkillType.None));
                if (MatchDetector.WouldCompleteMatch(grid, x, y))
                    excluded.Add(type);
                grid[x, y].RemovePetal();
            }

            PetalType[] candidates = System.Array.FindAll(allTypes, t => !excluded.Contains(t));
            if (candidates.Length == 0) candidates = allTypes;

            PetalType chosen = candidates[rng.Next(candidates.Length)];
            return PetalFactory.CreatePetal(chosen, SpecialSkillType.None);
        }
    }
}
