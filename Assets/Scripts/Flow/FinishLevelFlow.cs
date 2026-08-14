using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class FinishLevelFlow
    {
        private enum State
        {
            Inactive,
            BackgroundCaptured,
            Active
        }

        private readonly ConfigManager configManager;
        private readonly PlayFabLevelAttemptService levelAttemptService;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;
        private int? currentLevelId;
        private Texture2D capturedBackground;
        private State state;

        public event Action HomeRequested;
        public event Action<int> RetryRequested;
        public event Action<int> NextLevelRequested;

        public FinishLevelFlow(ConfigManager configManager, PlayFabLevelAttemptService levelAttemptService, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// [Duong] Captures the final gameplay frame
        /// </summary>
        public async UniTask CaptureBackground()
        {
            if (state != State.Inactive) throw new InvalidOperationException($"Cannot capture a level-result background while the finish flow is {state}.");

            await UniTask.WaitForEndOfFrame(UIManager.Instance);
            capturedBackground = ScreenCapture.CaptureScreenshotAsTexture();
            state = State.BackgroundCaptured;
        }

        /// <summary>
        /// [Duong] Submit level result to server
        /// </summary>
        public async UniTask<bool> TryEnter(LevelSessionResult result, string levelAttemptId)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (state != State.BackgroundCaptured) throw new InvalidOperationException($"Cannot enter the finish flow while it is {state}.");

            CompleteLevelAttemptResponse response;
            try
            {
                try
                {
                    response = await SubmitLevelCompletion(result, levelAttemptId);
                }
                catch (LevelCompletionSubmissionException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    response = await RunLevelCompletionRetryDialog(result, levelAttemptId);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                await RunInformationDialog(LevelCompletionDialogText.BackendFailureTitle, LevelCompletionDialogText.BackendFailureMessage);
                return false;
            }

            if (response.outcome == CompleteLevelAttemptOutcome.Rejected)
            {
                await RunInformationDialog(LevelCompletionDialogText.RejectionTitle, LevelCompletionDialogText.GetRejectionMessage(response.rejectionReason.Value));
                return false;
            }

            currentLevelId = result.LevelId;
            ShowResult(result);
            state = State.Active;
            return true;
        }

        /// <summary>
        /// [Duong] Leaves the current result presentation
        /// </summary>
        public void Exit()
        {
            if (state == State.Inactive) throw new InvalidOperationException("Cannot exit the finish flow when it is not active.");

            UIManager.Instance.WinScreenHomeRequested -= HandleHomeRequested;
            UIManager.Instance.WinScreenNextRequested -= HandleNextRequested;
            UIManager.Instance.LoseScreenRetryRequested -= HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested -= HandleHomeRequested;
            UIManager.Instance.HideWinScreen();
            UIManager.Instance.HideLoseScreen();
            currentLevelId = null;
            UnityEngine.Object.Destroy(capturedBackground);
            capturedBackground = null;
            state = State.Inactive;
        }

        /// <summary>
        /// [Duong] Tell server that we jus
        /// </summary>
        private async UniTask<CompleteLevelAttemptResponse> SubmitLevelCompletion(LevelSessionResult result, string levelAttemptId)
        {
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            CompleteLevelAttemptRequest request = new CompleteLevelAttemptRequest { attemptId = levelAttemptId, levelId = result.LevelId, didWin = result.DidWin, score = result.Score, stars = result.Stars };
            CompleteLevelAttemptResponse response = await ApplicationPresentationService.Instance.RunWithLoading(() => levelAttemptService.CompleteLevelAttempt(account.AuthSession, request));
            playerLivesPresentationService.ReplaceServerLivesSnapshot(response.lives);
            if (response.outcome == CompleteLevelAttemptOutcome.Saved)
                account.ApplyConfirmedLevelProgress(response.levelId, response.levelProgress, response.highestUnlockedLevel);
            return response;
        }

        private async UniTask<CompleteLevelAttemptResponse> RunLevelCompletionRetryDialog(LevelSessionResult result, string levelAttemptId)
        {
            CompleteLevelAttemptResponse response = null;
            DialogOptionButton[] options = { DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow(LevelCompletionDialogText.RetryTitle, LevelCompletionDialogText.RetryMessage, async session =>
            {
                while (true)
                {
                    int buttonId = await session.WaitForButtonClick();
                    if ((DialogButtonType)buttonId != DialogButtonType.Retry) throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported level completion failure dialog button.");

                    try
                    {
                        response = await SubmitLevelCompletion(result, levelAttemptId);
                        return;
                    }
                    catch (LevelCompletionSubmissionException exception) when (exception.IsRetryable)
                    {
                        Debug.LogWarning(exception);
                    }
                }
            }, options);

            if (response == null) throw new InvalidOperationException("Level completion retry dialog closed without a submission response.");
            return response;
        }

        private  async UniTask RunInformationDialog(string title, string message)
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow(title, message, async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok) throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported information dialog button.");
            }, options);
        }

        /// <summary>
        /// [Duong] Show result thru UI
        /// </summary>
        private void ShowResult(LevelSessionResult result)
        {
            if (result.DidWin)
            {
                bool showNext = TryGetNextUnlockedLevelId(result.LevelId, out _);
                UIManager.Instance.ShowWinScreen(capturedBackground, result.Stars, result.StarCap, showNext);
                UIManager.Instance.WinScreenHomeRequested += HandleHomeRequested;
                if (showNext)
                    UIManager.Instance.WinScreenNextRequested += HandleNextRequested;
                return;
            }

            UIManager.Instance.ShowLoseScreen(capturedBackground, result.FailureMessage);
            UIManager.Instance.LoseScreenRetryRequested += HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested += HandleHomeRequested;
        }

        /// <summary>
        /// [Duong] Find the next unlocked level
        /// </summary>
        private bool TryGetNextUnlockedLevelId(int levelId, out int nextLevelId)
        {
            if (!configManager.TryGetNextLevelId(levelId, out nextLevelId)) return false;
            if (nextLevelId > PlayerAccountContext.Instance.GetCurrentProgression().highestUnlockedLevel)
                throw new InvalidOperationException($"Level {nextLevelId} follows completed level {levelId} but is not unlocked by confirmed progression.");
            return true;
        }

        private void HandleRetryRequested()
        {
            if (!currentLevelId.HasValue) throw new InvalidOperationException("Cannot retry without an active finished level.");
            RetryRequested?.Invoke(currentLevelId.Value);
        }

        private void HandleHomeRequested()
        {
            HomeRequested?.Invoke();
        }

        private void HandleNextRequested()
        {
            if (!currentLevelId.HasValue) throw new InvalidOperationException("Cannot request the next level without an active winning result.");
            if (!TryGetNextUnlockedLevelId(currentLevelId.Value, out int nextLevelId)) throw new InvalidOperationException($"Cannot request a level after final level {currentLevelId.Value}.");
            NextLevelRequested?.Invoke(nextLevelId);
        }

    }
}
