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
            BackgroundShown,
            Active
        }

        private readonly ConfigManager configManager;
        private readonly PlayFabLevelAttemptService levelAttemptService;
        private readonly PlayFabInventoryService inventoryService;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;
        private int? currentLevelId;
        private Texture2D capturedBackground;
        private State state;

        public event Action HomeRequested;
        public event Action<int> RetryRequested;
        public event Action<int> NextLevelRequested;

        public FinishLevelFlow(ConfigManager configManager, PlayFabLevelAttemptService levelAttemptService, PlayFabInventoryService inventoryService, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
            this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// [Duong] Shows the final gameplay frame behind the level result
        /// </summary>
        public async UniTask ShowLevelResultBackground()
        {
            if (state != State.Inactive) throw new InvalidOperationException($"Cannot show a level-result background while the finish flow is {state}.");

            await UniTask.WaitForEndOfFrame(UIManager.Instance);
            capturedBackground = CaptureLevelResultBackground();
            UIManager.Instance.ShowBackground(capturedBackground);
            state = State.BackgroundShown;
        }

        private Texture2D CaptureLevelResultBackground()
        {
            Texture2D backgroundTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false, false);
            backgroundTexture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            backgroundTexture.Apply();
            return backgroundTexture;
        }

        /// <summary>
        /// [Duong] Submit level result to server
        /// </summary>
        public async UniTask<bool> TryEnter(LevelSessionResult result, string levelAttemptId)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (state != State.BackgroundShown) throw new InvalidOperationException($"Cannot enter the finish flow while it is {state}.");

            CompleteLevelAttemptResponse response;
            try
            {
                try
                {
                    response = await SubmitLevelCompletion(result, levelAttemptId);
                }
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
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
            ShowResult(result, response);
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
            UIManager.Instance.HideBackground();
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
            {
                account.ApplyConfirmedLevelProgress(response.levelId, response.levelProgress, response.highestUnlockedLevel);
                if (result.DidWin) account.ReplacePlayerInventory(inventoryService.CreatePlayerInventory(response.playerInventorySnapshot));
            }
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
                    catch (PlayFabRequestException exception) when (exception.IsRetryable)
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
        private void ShowResult(LevelSessionResult result, CompleteLevelAttemptResponse completionResponse)
        {
            if (result.DidWin)
            {
                bool showNext = TryGetNextUnlockedLevelId(result.LevelId, out _);
                RewardDisplayData rewardDisplayData = new RewardDisplayData(completionResponse.completionRewardPresentationKeys, result.Stars);
                UIManager.Instance.ShowWinScreen(result.Stars, result.StarCap, showNext, rewardDisplayData);
                UIManager.Instance.WinScreenHomeRequested += HandleHomeRequested;
                if (showNext)
                    UIManager.Instance.WinScreenNextRequested += HandleNextRequested;
                return;
            }

            UIManager.Instance.ShowLoseScreen(result.FailureMessage);
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
