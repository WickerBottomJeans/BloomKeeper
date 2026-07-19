using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;
using Utility;

namespace DefaultNamespace
{
    public class LevelSessionManager : Singleton<LevelSessionManager>
    {
        public event Action<LevelSessionResult> OnLevelFinished;

        private ObjectiveManager objectiveManager;
        private ConstrainerManager constrainerManager;
        [SerializeField] private WorldLevelBackground worldLevelBackgroundPrefab;
        [SerializeField] private GameBoard gameBoardPrefab;
        private WorldLevelBackground worldLevelBackgroundInstance;
        private GameBoard gameBoardInstance;
        private readonly List<ConstrainerFailureData> pendingConstrainerFailures = new();
        private ScoreManager scoreManager;
        private LevelData currentLevelData;
        private int currentLevelId;
        private bool pendingLevelComplete;
        private bool isLevelEnded;
        private bool isTurnSettling;
        private bool isLevelSessionPrepared;
        private bool isLevelSessionStarted;

        private void Update()
        {
            constrainerManager?.Tick(Time.deltaTime);
        }

        public void ClearCurrentLevelSession()
        {
            constrainerManager?.StopLevel();

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

            if (scoreManager != null)
                scoreManager.OnScoreChanged -= HandleScoreChanged;

            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnGameplayEvent -= HandleGameplayEvent;
                gameBoardInstance.OnTurnSettled -= HandleTurnSettled;
                Destroy(gameBoardInstance.gameObject);
                gameBoardInstance = null;
            }

            UIManager.Instance.HideLevelUI();

            if (worldLevelBackgroundInstance != null)
                worldLevelBackgroundInstance.Hide();

            objectiveManager = null;
            constrainerManager = null;
            scoreManager = null;
            currentLevelData = null;
            pendingConstrainerFailures.Clear();
            pendingLevelComplete = false;
            isLevelEnded = false;
            isTurnSettling = false;
            isLevelSessionPrepared = false;
            isLevelSessionStarted = false;
            currentLevelId = 0;
        }

        public void PrepareLevelSession(int levelId)
        {
            ClearCurrentLevelSession();

            currentLevelData = LevelLoader.LoadLevel(levelId);
            currentLevelId = levelId;
            scoreManager = new ScoreManager(currentLevelData.starScoreThresholds);
            scoreManager.OnScoreChanged += HandleScoreChanged;

            pendingConstrainerFailures.Clear();
            pendingLevelComplete = false;
            isLevelEnded = false;
            isTurnSettling = false;

            List<IObjective> objectives = currentLevelData.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();

            List<IConstrainer> constrainers = currentLevelData.constrainers
                .Select(c => ConstrainerFactory.Create(c))
                .ToList();

            objectiveManager = new ObjectiveManager(objectives);
            constrainerManager = new ConstrainerManager(constrainers);
            objectiveManager.OnAllComplete += HandleLevelComplete;
            objectiveManager.OnProgressUpdated += HandleObjectiveProgressUpdated;
            constrainerManager.OnFailed += HandleConstrainerFailed;
            constrainerManager.OnProgressUpdated += HandleConstrainerProgressUpdated;
            BoardCell[,] grid = BoardInitializer.Initialize(currentLevelData);
            
            DisplayLevel(grid, objectives, constrainerManager.GetViewData(), currentLevelData.starScoreThresholds);
            isLevelSessionPrepared = true;
        }

        public void StartPreparedLevelSession()
        {
            if (!isLevelSessionPrepared)
                throw new InvalidOperationException("Cannot start a level session before it has been prepared.");
            if (isLevelSessionStarted)
                throw new InvalidOperationException("Cannot start a level session more than once.");

            isLevelSessionStarted = true;
            constrainerManager.StartLevel();
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
            worldLevelBackgroundInstance.Show(Camera.main);
        }
        
        private void SpawnGameBoard(BoardCell[,] grid, Rect playAreaScreenRect)
        {
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

            if (isTurnSettling)
            {
                pendingConstrainerFailures.Add(failureData);
                return;
            }

            FinishLevelAsLoss(new List<ConstrainerFailureData> { failureData });
        }

        private void FinishLevelAsLoss(IReadOnlyList<ConstrainerFailureData> failureData)
        {
            if (isLevelEnded) return;

            isLevelEnded = true;
            constrainerManager?.StopLevel();
            if (failureData.Count == 0)
                throw new InvalidOperationException("Cannot show lose screen without constrainer failure data.");
            //TODO: maybe add a way to make multireason failure sound more fun
            string message = failureData[0].failureText;
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelId, false, scoreManager.CurrentScore, scoreManager.CalculateStars(), GetStarCap(currentLevelData.starScoreThresholds), message));
        }

        private void HandleGameplayEvent(IGameplayEvent e)
        {
            if (e is PlayerMoveCommittedEvent)
                isTurnSettling = true;

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

            isTurnSettling = false;

            if (pendingLevelComplete || objectiveManager.AllComplete)
            {
                FinishLevelAsWin();
                return;
            }

            if (pendingConstrainerFailures.Count == 0)
                return;

            FinishLevelAsLoss(pendingConstrainerFailures);
            pendingConstrainerFailures.Clear();
        }

        private void FinishLevelAsWin()
        {
            if (isLevelEnded) return;

            isLevelEnded = true;
            constrainerManager?.StopLevel();
            int earnedStars = scoreManager.CalculateStars();
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelId, true, scoreManager.CurrentScore, earnedStars, GetStarCap(currentLevelData.starScoreThresholds), string.Empty));
        }
    }
}
