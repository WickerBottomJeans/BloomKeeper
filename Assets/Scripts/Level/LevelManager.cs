using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelManager : Singleton<LevelManager>
    {
        private ObjectiveManager objectiveManager;
        public void InitNewLevel(int levelId)
        {
            LevelData data = LevelLoader.Load(levelId);

            List<IObjective> objectives = data.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();
            objectiveManager = new ObjectiveManager(objectives);
            objectiveManager.OnAllComplete += HandleLevelComplete;
            Tile[,] grid = new Tile[data.boardWidth, data.boardHeight];
            for (int i = 0; i < data.tiles.Count; i++)
            {
                int x = i % data.boardWidth;
                int y = data.boardHeight - 1 - (i / data.boardWidth);
                grid[x, y] = TileFactory.Create(data.tiles[i]);
            }
            
            UIManager.Instance.ShowScoreBoard();
            UIManager.Instance.ShowGameBoard(grid);
        }

        public void ReportCleared(List<PetalType> clearedPetals)
        {
            objectiveManager.Report(new PetalsClearedEvent(clearedPetals));
        }
        
        private void HandleLevelComplete()
        {
            UIManager.Instance.ShowWinScreen();
        }
    }
}