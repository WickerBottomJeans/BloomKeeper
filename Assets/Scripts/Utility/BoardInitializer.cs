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

        public static BoardCell[,] Initialize(LevelData data)
        {
            int cols = data.boardWidth;
            int rows = data.boardHeight;
            BoardCell[,] grid = new BoardCell[cols, rows];

            for (int i = 0; i < data.tiles.Count; i++)
            {
                int x = i % cols;
                int y = rows - 1 - (i / cols);
                TileData tileData = data.tiles[i];
                grid[x, y] = tileData.isVoid
                    ? new BoardCell(true, null)
                    : new BoardCell(false, TileFactory.Create(tileData));
            }

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    int index = (rows - 1 - y) * cols + x;
                    TileData tileData = index < data.tiles.Count ? data.tiles[index] : new TileData();
                    BoardCell cell = grid[x, y];

                    if (cell.IsVoid || cell.Tile is InactiveTile) continue;

                    cell.Petal = tileData.petalType != PetalType.None
                        ? PetalFactory.CreateForTileMap(tileData)
                        : CreatePetalWithConstrained(grid, x, y);
                }
            }

            return grid;
        }

        private static Petal CreatePetalWithConstrained(BoardCell[,] grid, int x, int y)
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
