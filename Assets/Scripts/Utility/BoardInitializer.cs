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
            if (cols <= 0 || rows <= 0 || data.tiles == null || data.tiles.Count != (long)cols * rows)
                throw new System.ArgumentException("The board needs positive dimensions and one tiles entry per cell, including null for holes.", nameof(data));
            Tile[,] grid = new Tile[cols, rows];

            for (int i = 0; i < data.tiles.Count; i++)
            {
                int x = i % cols;
                int y = rows - 1 - (i / cols);
                TileData tileData = data.tiles[i];
                grid[x, y] = tileData == null ? null : TileFactory.Create(tileData);
            }

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    int index = (rows - 1 - y) * cols + x;
                    TileData tileData = data.tiles[index];
                    Tile tile = grid[x, y];

                    if (tile == null || !tile.CanReceivePiece()) continue;

                    if (tileData.hasEgg)
                    {
                        tile.SetPiece(new Egg());
                        continue;
                    }

                    tile.SetPiece(tileData.skillType == SpecialSkillType.PrismaticBloom || tileData.petalType != PetalType.None
                        ? PetalFactory.CreateForTileMap(tileData)
                        : CreatePetalWithConstrained(grid, x, y));
                }
            }

            if (EggCollectionDetector.Detect(grid).Count > 0)
                throw new System.InvalidOperationException("An egg cannot start beside a basket.");
            return grid;
        }

        private static Petal CreatePetalWithConstrained(Tile[,] grid, int x, int y)
        {
            PetalType[] allTypes = PetalFactory.RandomPetalTypes;
            List<PetalType> excluded = new List<PetalType>();

            foreach (PetalType type in allTypes)
            {
                grid[x, y].SetPiece(PetalFactory.CreatePetal(type, SpecialSkillType.None));
                if (MatchDetector.WouldCompleteMatch(grid, x, y))
                    excluded.Add(type);
                grid[x, y].RemovePiece();
            }

            PetalType[] candidates = System.Array.FindAll(allTypes, t => !excluded.Contains(t));
            if (candidates.Length == 0) candidates = allTypes;

            PetalType chosen = candidates[rng.Next(candidates.Length)];
            return PetalFactory.CreatePetal(chosen, SpecialSkillType.None);
        }
    }
}
