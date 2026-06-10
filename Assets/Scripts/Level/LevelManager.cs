using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;
using Utility;

namespace DefaultNamespace
{
    public class LevelManager : Singleton<LevelManager>
    {
        private ObjectiveManager objectiveManager;
        [SerializeField] private GameBoard gameBoardPrefab;
        private GameBoard gameBoardInstance;
        public void InitNewLevel(int levelId)
        {
            LevelData data = LevelLoader.LoadLevel(levelId);

            List<IObjective> objectives = data.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();

            if (objectiveManager != null)
            {
                objectiveManager.OnAllComplete -= HandleLevelComplete;
                objectiveManager.OnProgressUpdated -= HandleObjectiveProgressUpdated;
            }

            objectiveManager = new ObjectiveManager(objectives);
            objectiveManager.OnAllComplete += HandleLevelComplete;
            objectiveManager.OnProgressUpdated += HandleObjectiveProgressUpdated;
            Tile[,] grid = BoardInitializer.Initialize(data);
            
            UIManager.Instance.ShowScoreBoard(objectives);
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

        private void HandleObjectiveProgressUpdated()
        {
            UIManager.Instance.RefreshObjectiveOnScoreBoard();
        }
    }
}