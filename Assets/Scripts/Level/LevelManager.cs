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
        private ConstrainerManager constrainerManager;
        [SerializeField] private GameBoard gameBoardPrefab;
        private GameBoard gameBoardInstance;
        private readonly List<ConstrainerFailureData> pendingConstrainerFailures = new();
        private bool pendingLevelComplete;
        private bool isLevelEnded;

        private void Update()
        {
            constrainerManager?.Tick(Time.deltaTime);
        }

        public void InitNewLevel(int levelId)
        {
            LevelData data = LevelLoader.LoadLevel(levelId);

            constrainerManager?.StopLevel();
            pendingConstrainerFailures.Clear();
            pendingLevelComplete = false;
            isLevelEnded = false;

            List<IObjective> objectives = data.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();

            List<IConstrainer> constrainers = data.constrainers
                .Select(c => ConstrainerFactory.Create(c))
                .ToList();

            if (objectiveManager != null)
            {
                objectiveManager.OnAllComplete -= HandleLevelComplete;
                objectiveManager.OnProgressUpdated -= HandleObjectiveProgressUpdated;
            }
            
            if (constrainerManager != null)
            {
                constrainerManager.OnFailed -= HandleConstrainerFailed;
                constrainerManager.OnProgressUpdated -= HandleConstrainerProgressUpdated;
            }

            objectiveManager = new ObjectiveManager(objectives);
            constrainerManager = new ConstrainerManager(constrainers);
            constrainerManager.StartLevel();
            objectiveManager.OnAllComplete += HandleLevelComplete;
            objectiveManager.OnProgressUpdated += HandleObjectiveProgressUpdated;
            constrainerManager.OnFailed += HandleConstrainerFailed;
            constrainerManager.OnProgressUpdated += HandleConstrainerProgressUpdated;
            BoardCell[,] grid = BoardInitializer.Initialize(data);
            
            UIManager.Instance.ShowScoreBoard(objectives, constrainerManager.GetViewData());
            SpawnGameBoard(grid);
        }
        
        private void SpawnGameBoard(BoardCell[,] grid)
        {
            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnGameplayEvent -= HandleGameplayEvent;
                gameBoardInstance.OnTurnSettled -= HandleTurnSettled;
                Destroy(gameBoardInstance.gameObject);
            }

            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid);
            gameBoardInstance.OnGameplayEvent += HandleGameplayEvent;
            gameBoardInstance.OnTurnSettled += HandleTurnSettled;
        }
        
        private void HandleLevelComplete()
        {
            if (isLevelEnded) return;

            pendingLevelComplete = true;
        }

        private void HandleObjectiveProgressUpdated()
        {
            UIManager.Instance.RefreshObjectiveOnScoreBoard();
        }

        private void HandleConstrainerProgressUpdated()
        {
            UIManager.Instance.RefreshConstrainersOnScoreBoard(constrainerManager.GetViewData());
        }

        private void HandleConstrainerFailed(ConstrainerFailureData failureData)
        {
            if (isLevelEnded) return;

            pendingConstrainerFailures.Add(failureData);
        }

        private void ShowLoseForConstrainers(IReadOnlyList<ConstrainerFailureData> failureData)
        {
            if (isLevelEnded) return;

            isLevelEnded = true;
            constrainerManager?.StopLevel();
            if (failureData.Count == 0)
                throw new InvalidOperationException("Cannot show lose screen without constrainer failure data.");
            //TODO: maybe add a way to make multireason failure sound more fun
            string message = failureData[0].failureText;
            UIManager.Instance.ShowLoseScreen(message);
        }

        private void HandleGameplayEvent(IGameplayEvent e)
        {
            objectiveManager.Report(e);
            constrainerManager.Apply(e);
        }

        private void HandleTurnSettled()
        {
            if (isLevelEnded) return;

            if (pendingLevelComplete || objectiveManager.AllComplete)
            {
                ShowWin();
                return;
            }

            if (pendingConstrainerFailures.Count == 0)
                return;

            ShowLoseForConstrainers(pendingConstrainerFailures);
            pendingConstrainerFailures.Clear();
        }

        private void ShowWin()
        {
            if (isLevelEnded) return;

            isLevelEnded = true;
            constrainerManager?.StopLevel();
            UIManager.Instance.ShowWinScreen();
        }
    }
}
