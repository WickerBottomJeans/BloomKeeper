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
        [SerializeField] private WorldLevelBackground worldLevelBackgroundPrefab;
        [SerializeField] private GameBoard gameBoardPrefab;
        private WorldLevelBackground worldLevelBackgroundInstance;
        private GameBoard gameBoardInstance;
        private readonly List<ConstrainerFailureData> pendingConstrainerFailures = new();
        private ScoreManager scoreManager;
        private int currentLevelId;
        private bool pendingLevelComplete;
        private bool isLevelEnded;

        private void Update()
        {
            constrainerManager?.Tick(Time.deltaTime);
        }

        public void InitNewLevel(int levelId)
        {
            LevelData data = LevelLoader.LoadLevel(levelId);
            currentLevelId = levelId;
            if (scoreManager != null)
                scoreManager.OnScoreChanged -= HandleScoreChanged;
            scoreManager = new ScoreManager(data.starScoreThresholds);
            scoreManager.OnScoreChanged += HandleScoreChanged;

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
            
            DisplayLevel(grid, objectives, constrainerManager.GetViewData(), data.starScoreThresholds);
        }

        private void DisplayLevel(BoardCell[,] grid, List<IObjective> objectives, List<ConstrainerViewData> constrainerViewData, IReadOnlyList<StarScoreThresholdJson> starScoreThresholds)
        {
            ShowWorldLevelBackground();
            int scoreTarget = GetScoreTarget(starScoreThresholds);
            List<int> scoreMilestones = GetScoreMilestones(starScoreThresholds);
            int starCap = GetStarCap(starScoreThresholds);
            UIManager.Instance.ShowLevelUI(objectives, constrainerViewData, scoreTarget, scoreMilestones, starCap);
            SpawnGameBoard(grid, UIManager.Instance.GetLevelBoardPlayAreaScreenRect());
        }

        private void ShowWorldLevelBackground()
        {
            if (worldLevelBackgroundPrefab == null) return;
            if (worldLevelBackgroundInstance == null)
                worldLevelBackgroundInstance = Instantiate(worldLevelBackgroundPrefab);
            worldLevelBackgroundInstance.gameObject.SetActive(true);
            worldLevelBackgroundInstance.FitWidthToCamera(Camera.main);
        }
        
        private void SpawnGameBoard(BoardCell[,] grid, Rect playAreaScreenRect)
        {
            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnGameplayEvent -= HandleGameplayEvent;
                gameBoardInstance.OnTurnSettled -= HandleTurnSettled;
                Destroy(gameBoardInstance.gameObject);
            }

            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid, playAreaScreenRect);
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
            UIManager.Instance.RefreshLevelObjectives();
        }

        private void HandleConstrainerProgressUpdated()
        {
            UIManager.Instance.RefreshLevelConstrainers(constrainerManager.GetViewData());
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
            if (e is BoardResolvedEvent boardResolvedEvent)
                scoreManager.Apply(boardResolvedEvent);

            objectiveManager.Report(e);
            constrainerManager.Apply(e);
        }

        private void HandleScoreChanged(int currentScore, int currentStars)
        {
            UIManager.Instance.DisplayLevelScore(currentScore, currentStars);
        }

        private static int GetScoreTarget(IReadOnlyList<StarScoreThresholdJson> starScoreThresholds)
        {
            int target = 0;
            foreach (StarScoreThresholdJson threshold in starScoreThresholds)
            {
                if (threshold.score > target)
                    target = threshold.score;
            }

            return target;
        }

        private static List<int> GetScoreMilestones(IReadOnlyList<StarScoreThresholdJson> starScoreThresholds)
        {
            int target = GetScoreTarget(starScoreThresholds);
            List<int> milestones = new();
            foreach (StarScoreThresholdJson threshold in starScoreThresholds)
            {
                if (threshold.score > 0 && threshold.score < target)
                    milestones.Add(threshold.score);
            }

            return milestones;
        }

        private static int GetStarCap(IReadOnlyList<StarScoreThresholdJson> starScoreThresholds)
        {
            int starCap = 0;
            foreach (StarScoreThresholdJson threshold in starScoreThresholds)
            {
                if (threshold.starCount > starCap)
                    starCap = threshold.starCount;
            }

            return starCap;
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
            int earnedStars = scoreManager.CalculateStars();
            int previousStars = PlayerProgress.Instance.GetStars(currentLevelId);
            if (earnedStars > previousStars)
                PlayerProgress.Instance.SetStars(currentLevelId, earnedStars);
            UIManager.Instance.ShowWinScreen();
        }
    }
}
