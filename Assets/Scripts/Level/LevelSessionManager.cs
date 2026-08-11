using System;
using System.Collections.Generic;
using System.Linq;
using Boosters;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;
using Utility;

namespace DefaultNamespace
{
    public class LevelSessionManager : Singleton<LevelSessionManager>
    {
        public event Action<LevelSessionResult> OnLevelFinished;
        public event Action RecoverableOperationFailed;

        private ObjectiveManager objectiveManager;
        private ConstrainerManager constrainerManager;
        [SerializeField] private WorldLevelBackground worldLevelBackgroundPrefab;
        [SerializeField] private GameBoard gameBoardPrefab;
        private BoosterUseCoordinator boosterUseCoordinator;
        private WorldLevelBackground worldLevelBackgroundInstance;
        private GameBoard gameBoardInstance;
        private readonly List<ConstrainerFailureData> pendingConstrainerFailures = new();
        private ScoreManager scoreManager;
        private LevelData currentLevelData;
        private int currentLevelId;
        private string currentLevelAttemptId;
        private bool pendingLevelComplete;
        private bool isLevelEnded;
        private bool isTurnSettling;
        private bool isLevelSessionPrepared;
        private bool isLevelSessionStarted;
        private bool isLevelSessionPaused;
        private bool isBoosterAuthorizationPauseActive;

        private void Update()
        {
            constrainerManager?.Tick(Time.deltaTime);
        }

        public void ClearCurrentLevelSession()
        {
            if (boosterUseCoordinator != null)
            {
                boosterUseCoordinator.BoosterTargetingApproved -= HandleBoosterTargetingApproved;
                boosterUseCoordinator.BoosterCancelApproved -= HandleBoosterCancelApproved;
            }
            UIManager.Instance.BoosterUseRequested -= HandleBoosterUseRequested;
            UIManager.Instance.BoosterCancelRequested -= HandleBoosterCancelRequested;
            constrainerManager?.StopLevel();
            if (isLevelSessionPaused)
                GameTimeService.ReleasePause(this);
            if (isBoosterAuthorizationPauseActive)
                GameTimeService.ReleasePause(boosterUseCoordinator);

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
                gameBoardInstance.OnBoardSettled -= HandleBoardSettled;
                gameBoardInstance.BoosterUseFailed -= HandleBoosterUseFailed;
                gameBoardInstance.BoosterTargetsSelected -= HandleBoosterTargetsSelected;
                gameBoardInstance.BoosterTargetingCanceled -= HandleBoosterTargetingCanceled;
                Destroy(gameBoardInstance.gameObject);
                gameBoardInstance = null;
            }

            UIManager.Instance.HideLevelUI();

            if (worldLevelBackgroundInstance != null)
                worldLevelBackgroundInstance.Hide();

            objectiveManager = null;
            constrainerManager = null;
            boosterUseCoordinator = null;
            scoreManager = null;
            currentLevelData = null;
            pendingConstrainerFailures.Clear();
            pendingLevelComplete = false;
            isLevelEnded = false;
            isTurnSettling = false;
            isLevelSessionPrepared = false;
            isLevelSessionStarted = false;
            isLevelSessionPaused = false;
            isBoosterAuthorizationPauseActive = false;
            currentLevelId = 0;
            currentLevelAttemptId = null;
        }

        public void PrepareLevelSession(LevelData levelData)
        {
            ClearCurrentLevelSession();

            currentLevelData = levelData;
            currentLevelId = levelData.levelId;
            currentLevelAttemptId = Guid.NewGuid().ToString("N");
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
            boosterUseCoordinator = new BoosterUseCoordinator(currentLevelData.allowedBoosters, new PlayFabBoosterInventoryService());
            objectiveManager.OnAllComplete += HandleLevelComplete;
            objectiveManager.OnProgressUpdated += HandleObjectiveProgressUpdated;
            constrainerManager.OnFailed += HandleConstrainerFailed;
            constrainerManager.OnProgressUpdated += HandleConstrainerProgressUpdated;
            Tile[,] grid = BoardInitializer.Initialize(currentLevelData);
            var levelUIInitData = new LevelUIInitData(objectiveManager.GetViewData(), constrainerManager.GetViewData(), scoreManager.GetViewData(), boosterUseCoordinator.GetViewData());
            DisplayLevel(grid, levelUIInitData);
            isLevelSessionPrepared = true;
        }

        public void StartPreparedLevelSession()
        {
            if (!isLevelSessionPrepared)
                throw new InvalidOperationException("Cannot start a level session before it has been prepared.");
            if (isLevelSessionStarted)
                throw new InvalidOperationException("Cannot start a level session more than once.");

            isLevelSessionStarted = true;
            isLevelSessionPaused = false;
            boosterUseCoordinator.BoosterTargetingApproved += HandleBoosterTargetingApproved;
            boosterUseCoordinator.BoosterCancelApproved += HandleBoosterCancelApproved;
            UIManager.Instance.BoosterUseRequested += HandleBoosterUseRequested;
            UIManager.Instance.BoosterCancelRequested += HandleBoosterCancelRequested;
            constrainerManager.StartLevel();
        }

        public void PauseCurrentLevelSession()
        {
            if (!isLevelSessionStarted)
                throw new InvalidOperationException("Cannot pause a level session before it has started.");
            if (isLevelEnded)
                throw new InvalidOperationException("Cannot pause a level session after it has ended.");
            if (isLevelSessionPaused)
                throw new InvalidOperationException("Cannot pause a level session that is already paused.");

            isLevelSessionPaused = true;
            constrainerManager.StopLevel();
            GameTimeService.RequestPause(this);
        }

        public void ResumeCurrentLevelSession()
        {
            if (!isLevelSessionStarted)
                throw new InvalidOperationException("Cannot resume a level session before it has started.");
            if (isLevelEnded)
                throw new InvalidOperationException("Cannot resume a level session after it has ended.");
            if (!isLevelSessionPaused)
                throw new InvalidOperationException("Cannot resume a level session that is not paused.");

            GameTimeService.ReleasePause(this);
            isLevelSessionPaused = false;
            if (!isBoosterAuthorizationPauseActive)
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
            gameBoardInstance.OnBoardSettled += HandleBoardSettled;
            gameBoardInstance.BoosterUseFailed += HandleBoosterUseFailed;
            gameBoardInstance.BoosterTargetsSelected += HandleBoosterTargetsSelected;
            gameBoardInstance.BoosterTargetingCanceled += HandleBoosterTargetingCanceled;
        }

        private IReadOnlyList<ObjectiveTileTargetGroup> ResolveObjectiveTargets(IReadOnlyList<TileState> boardSnapshot)
        {
            return objectiveManager.GetTargetGroups(boardSnapshot);
        }
        
        private void HandleLevelComplete()
        {
            if (isLevelEnded) return;

            pendingLevelComplete = true;
        }

        private async UniTask RetryPendingBoosterAuthorization()
        {
            BoosterAuthorizationResult result = await RequestBoosterAuthorizationRetry();
            CompletePendingBoosterAuthorization(result);
        }

        private void FailPendingBoosterAuthorizationAfterRetry(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Debug.LogWarning(exception);
            FailPendingBoosterAuthorization();
        }

        private void HandleBoosterTargetingApproved(BoosterType boosterType)
        {
            UIManager.Instance.EnterBoosterTargeting(boosterType);
            gameBoardInstance.BeginBoosterTargeting(boosterType);
        }

        private void HandleBoosterCancelApproved()
        {
            gameBoardInstance.CancelBoosterTargeting();
        }

        private void HandleBoosterUseRequested(BoosterType boosterType)
        {
            boosterUseCoordinator.RequestUse(boosterType);
        }

        private void HandleBoosterCancelRequested()
        {
            boosterUseCoordinator.RequestCancel();
        }

        private void HandleBoosterTargetingCanceled()
        {
            boosterUseCoordinator.CompleteTargetingCancellation();
            UIManager.Instance.ExitBoosterTargeting();
        }

        private void HandleBoosterTargetsSelected(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            UIManager.Instance.EnterBoosterAuthorizationPending();
            BeginBoosterAuthorizationPause();
            RunInitialBoosterAuthorization(boosterType, targets).Forget();
        }

        /// <summary>
        /// Asks the server to authorize the use of this booster
        /// </summary>
        /// <param name="boosterType"></param>
        /// <param name="targets"></param>
        private async UniTask RunInitialBoosterAuthorization(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            BoosterAuthorizationResult result;
            try
            {
                //Request booster consume to server
                result = await ApplicationPresentationService.Instance.RunWithLoading(() => boosterUseCoordinator.AuthorizeBoosterUse(boosterType, targets).AsTask());
            }
            //Have problem but still let retry
            catch (BoosterConsumptionException exception) when (exception.IsRetryable)
            {
                Debug.LogWarning(exception);
                ApplicationOperationRunner.Instance.Run(RunBoosterAuthorizationRetryDialogAsync);
                return;
            }
            //Fail
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                FailPendingBoosterAuthorization();
                return;
            }

            CompletePendingBoosterAuthorization(result);
        }

        private async UniTask RunBoosterAuthorizationRetryDialogAsync()
        {
            Exception terminalFailure = null;
            DialogOptionButton[] options = { DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow("Connection interrupted", "The booster has not been applied yet. Retry to safely confirm the same use.", async session =>
            {
                while (true)
                {
                    int buttonId = await session.WaitForButtonClick();
                    if ((DialogButtonType)buttonId != DialogButtonType.Retry) throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported booster authorization retry button.");

                    try
                    {
                        await RetryPendingBoosterAuthorization();
                        return;
                    }
                    catch (BoosterConsumptionException exception) when (exception.IsRetryable)
                    {
                    }
                    catch (Exception exception)
                    {
                        terminalFailure = exception;
                        return;
                    }
                }
            }, options);

            if (terminalFailure != null)
                FailPendingBoosterAuthorizationAfterRetry(terminalFailure);
        }

        private UniTask<BoosterAuthorizationResult> RequestBoosterAuthorizationRetry()
        {
            return ApplicationPresentationService.Instance.RunWithLoading(() => boosterUseCoordinator.RetryPendingAuthorization().AsTask());
        }

        /// <summary>
        /// When the request to use a booster has a response
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void CompletePendingBoosterAuthorization(BoosterAuthorizationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            UIManager.Instance.RefreshLevelBoosters(boosterUseCoordinator.GetViewData());
            UIManager.Instance.ExitBoosterTargeting();

            try
            {
                if (result.Consumed)
                    gameBoardInstance.ExecuteApprovedBooster(result.BoosterType, result.Targets);
                else
                    gameBoardInstance.RejectPendingBoosterUse();
            }
            finally
            {
                EndBoosterAuthorizationPause();
            }
        }

        private void FailPendingBoosterAuthorization()
        {
            boosterUseCoordinator.AbandonPendingUse();
            UIManager.Instance.ExitBoosterTargeting();
            gameBoardInstance.RejectPendingBoosterUse();
            EndBoosterAuthorizationPause();
            RecoverableOperationFailed?.Invoke();
        }

        private void BeginBoosterAuthorizationPause()
        {
            if (isBoosterAuthorizationPauseActive) throw new InvalidOperationException("Booster authorization already owns a game-time pause.");

            constrainerManager.StopLevel();
            GameTimeService.RequestPause(boosterUseCoordinator);
            isBoosterAuthorizationPauseActive = true;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
        }

        private void EndBoosterAuthorizationPause()
        {
            if (!isBoosterAuthorizationPauseActive) throw new InvalidOperationException("Booster authorization does not own a game-time pause.");

            GameTimeService.ReleasePause(boosterUseCoordinator);
            isBoosterAuthorizationPauseActive = false;
            if (!isLevelSessionPaused && !isLevelEnded)
            {
                constrainerManager.StartLevel();
                ApplicationInputController.Instance.SetGameBoardInputActive(true);
            }
        }

        private void HandleBoosterUseFailed()
        {
            RecoverableOperationFailed?.Invoke();
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
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelId, currentLevelAttemptId, false, scoreManager.CurrentScore, scoreManager.CalculateStars(), currentLevelData.StarCap, message));
        }

        private void HandleGameplayEvent(IGameplayEvent e)
        {
            if (e is PlayerMoveCommittedEvent)
                isTurnSettling = true;

            if (e is BoardResolvedEvent boardResolvedEvent)
            {
                scoreManager.Apply(boardResolvedEvent);
                objectiveManager.Apply(boardResolvedEvent.Result.TileChanges);
            }

            constrainerManager.Apply(e);
        }

        private void HandleScoreChanged(int currentScore, int currentStars)
        {
            UIManager.Instance.DisplayLevelScore(currentScore, currentStars);
        }

        private void HandleBoardSettled()
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
            OnLevelFinished?.Invoke(new LevelSessionResult(currentLevelId, currentLevelAttemptId, true, scoreManager.CurrentScore, earnedStars, currentLevelData.StarCap, string.Empty));
        }
    }
}
