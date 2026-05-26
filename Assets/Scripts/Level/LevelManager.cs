using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelManager : Singleton<LevelManager>
    {
        private List<IObjective> objectives;
        private Tile[,] grid;

        public void InitNewLevel(int levelId)
        {
            LevelData data = LevelLoader.Load(levelId);

            objectives = data.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();

            grid = new Tile[data.boardWidth, data.boardHeight];
            for (int i = 0; i < data.tiles.Count; i++)
            {
                int x = i % data.boardWidth;
                int y = i / data.boardWidth;
                grid[x, y] = TileFactory.Create(data.tiles[i]);
            }

            UIManager.Instance.ShowGameBoard(grid);
        }
    }
}