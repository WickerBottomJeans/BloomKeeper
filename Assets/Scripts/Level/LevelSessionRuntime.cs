using System;
using System.Collections.Generic;
using System.Linq;
using Boosters;
using DefaultNamespace.UI;
using UnityEngine;
using Utility;

namespace DefaultNamespace
{
    public class LevelSessionRuntime : MonoBehaviour
    {
        public event Action<LevelSessionResult> OnLevelFinished;

        private ObjectiveManager objectiveManager;
        private ConstrainerManager constrainerManager;
        [SerializeField] private WorldLevelBackground worldLevelBackgroundPrefab;
        [SerializeField] private GameBoard gameBoardPrefab;
        private BoosterUseService boosterUseService;
        private BoosterFlow boosterFlow;
        private WorldLevelBackground worldLevelBackgroundInstance;
        private GameBoard gameBoardInstance;
        private LevelOutcomeDecider levelOutcomeDecider;
        private GameplayEventDispatcher gameplayEventDispatcher;
        private ScoreManager scoreManager;
        private LevelData currentLevelData;

        private void Update()
        {
            constrainerManager?.Tick(Time.deltaTime);
        }

        public void ClearCurrentLevelSession()
        {
            boosterFlow?.Stop();
            boosterFlow = null;
            constrainerManager?.StopLevel();

            if (objectiveManager != null)
            {
                objectiveManager.OnAllObjectedCompleted -= HandleAllObjectivesCompleted;
                objectiveManager.OnProgressUpdated -= HandleObjectiveProgressUpdated;
            }

            if (constrainerManager != null)
            {
                constrainerManager.OnFailed -= HandleConstrainerFailed;
                constrainerManager.OnProgressUpdated -= HandleConstrainerProgressUpdated;
            }

            if (scoreManager != null)
                scoreManager.OnScoreChanged -= HandleScoreChanged;

            if (levelOutcomeDecider != null)
            {
                levelOutcomeDecider.WinDecided -= HandleWinDecided;
                levelOutcomeDecider.LossDecided -= HandleLossDecided;
            }

            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnGameplayEvent -= HandleGameplayEvent;
                gameBoardInstance.IdleStateChanged -= HandleBoardIdleStateChanged;
                Destroy(gameBoardInstance.gameObject);
                gameBoardInstance = null;
            }

            UIManager.Instance.HideLevelUI();

            if (worldLevelBackgroundInstance != null)
                worldLevelBackgroundInstance.Hide();

            objectiveManager = null;
            constrainerManager = null;
            boosterUseService = null;
            levelOutcomeDecider = null;
            gameplayEventDispatcher = null;
            scoreManager = null;
            currentLevelData = null;
        }

        public void PrepareLevelSession(LevelData levelData)
        {
            if (levelData == null) throw new ArgumentNullException(nameof(levelData));

            ClearCurrentLevelSession();

            currentLevelData = levelData;
            gameplayEventDispatcher = new GameplayEventDispatcher();
            scoreManager = new ScoreManager(currentLevelData.starScoreThresholds);
            gameplayEventDispatcher.Register(scoreManager);
            scoreManager.OnScoreChanged += HandleScoreChanged;

            List<IObjective> objectives = currentLevelData.objectives.Select(o => ObjectiveFactory.Create(o)).ToList();
            List<IConstrainer> constrainers = currentLevelData.constrainers.Select(c => ConstrainerFactory.Create(c)).ToList();

            objectiveManager = new ObjectiveManager(objectives);
            constrainerManager = new ConstrainerManager(constrainers);
            gameplayEventDispatcher.Register(objectiveManager);
            gameplayEventDispatcher.Register(constrainerManager);
            boosterUseService = new BoosterUseService(currentLevelData.allowedBoosters, new PlayFabBoosterInventoryService());
            levelOutcomeDecider = new LevelOutcomeDecider();
            levelOutcomeDecider.WinDecided += HandleWinDecided;
            levelOutcomeDecider.LossDecided += HandleLossDecided;
            objectiveManager.OnAllObjectedCompleted += HandleAllObjectivesCompleted;
            objectiveManager.OnProgressUpdated += HandleObjectiveProgressUpdated;
            constrainerManager.OnFailed += HandleConstrainerFailed;
            constrainerManager.OnProgressUpdated += HandleConstrainerProgressUpdated;
            Tile[,] grid = BoardInitializer.Initialize(currentLevelData);
            var levelUIInitData = new LevelUIInitData(objectiveManager.GetViewData(), constrainerManager.GetViewData(), scoreManager.GetViewData(), boosterUseService.GetViewData());
            DisplayLevel(grid, levelUIInitData);
            boosterFlow = new BoosterFlow(boosterUseService, gameBoardInstance);
        }

        public void StartPreparedLevelSession()
        {
            boosterFlow.Start();
            constrainerManager.StartLevel();
        }

        public void PauseCurrentLevelSession()
        {
            constrainerManager.StopLevel();
        }

        public void ResumeCurrentLevelSession()
        {
            constrainerManager.StartLevel();
        }

        private void DisplayLevel(Tile[,] grid, LevelUIInitData levelUIInitData)
        {
            ShowWorldLevelBackground();
            UIManager.Instance.ShowLevelUI(levelUIInitData);
            SpawnGameBoard(grid, UIManager.Instance.GetLevelBoardPlayAreaScreenRect());
        }

        private void ShowWorldLevelBackground()
        {
            if (worldLevelBackgroundPrefab == null) return;
            if (worldLevelBackgroundInstance == null)
                worldLevelBackgroundInstance = Instantiate(worldLevelBackgroundPrefab);
            worldLevelBackgroundInstance.Show(Camera.main);
        }

        private void SpawnGameBoard(Tile[,] grid, Rect playAreaScreenRect)
        {
            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid, playAreaScreenRect, ResolveObjectiveTargets);
            gameBoardInstance.OnGameplayEvent += HandleGameplayEvent;
            gameBoardInstance.IdleStateChanged += HandleBoardIdleStateChanged;
            levelOutcomeDecider.HandleBoardIdleStateChanged(gameBoardInstance.IsIdle);
        }

        private IReadOnlyList<ObjectiveTileTargetGroup> ResolveObjectiveTargets(IReadOnlyList<TileState> boardSnapshot)
        {
            return objectiveManager.GetTargetGroups(boardSnapshot);
        }

        private void HandleAllObjectivesCompleted()
        {
            levelOutcomeDecider.HandleAllObjectivesCompleted();
        }

        private void HandleObjectiveProgressUpdated()
        {
            UIManager.Instance.RefreshLevelObjectives(objectiveManager.GetViewData());
        }

        private void HandleConstrainerProgressUpdated()
        {
            UIManager.Instance.RefreshLevelConstrainers(constrainerManager.GetViewData());
        }

        private void HandleConstrainerFailed(ConstrainerFailureData failureData)
        {
            levelOutcomeDecider.HandleConstrainerFailure(failureData);
        }

        private void HandleLossDecided(IReadOnlyList<ConstrainerFailureData> failureData)
        {
            constrainerManager?.StopLevel();
            if (failureData.Count == 0)
                throw new InvalidOperationException("Cannot show lose screen without constrainer failure data.");
            //TODO: maybe add a way to make multireason failure sound more fun
            string message = failureData[0].failureText;
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelData.levelId, false, scoreManager.CurrentScore, scoreManager.CalculateStars(), currentLevelData.StarCap, message));
        }

        private void HandleBoardIdleStateChanged(bool isIdle)
        {
            levelOutcomeDecider.HandleBoardIdleStateChanged(isIdle);
        }

        private void HandleGameplayEvent(IGameplayEvent e)
        {
            gameplayEventDispatcher.Dispatch(e);
        }

        private void HandleScoreChanged(int currentScore, int currentStars)
        {
            UIManager.Instance.DisplayLevelScore(currentScore, currentStars);
        }

        private void HandleWinDecided()
        {
            constrainerManager?.StopLevel();
            int earnedStars = scoreManager.CalculateStars();
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelData.levelId, true, scoreManager.CurrentScore, earnedStars, currentLevelData.StarCap, string.Empty));
        }
    }
}
