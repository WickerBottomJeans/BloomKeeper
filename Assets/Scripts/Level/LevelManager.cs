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
        [SerializeField] private GameBoard gameBoardPrefab;
        private GameBoard gameBoardInstance;
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
            SpawnGameBoard(grid);
        }
        
        private void SpawnGameBoard(Tile[,] grid)
        {
            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnPetalsCleared -= ReportCleared;
                Destroy(gameBoardInstance.gameObject);
            }

            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid);
            gameBoardInstance.OnPetalsCleared += ReportCleared;
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