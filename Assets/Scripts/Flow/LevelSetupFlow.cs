using System;
using System.IO;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Owns only pre-level setup: loads the level config, obtains server approval, and resolves failures that block entry.
    /// Does not start, run, or finish the local level session.
    /// </summary>
    public class LevelSetupFlow
    {
        private readonly ConfigManager configManager;
        private readonly PlayFabLevelAttemptService levelAttemptService;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;

        public LevelSetupFlow(ConfigManager configManager, PlayFabLevelAttemptService levelAttemptService, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// [Duong] Tries to obtain server approval and the config required to set up a level.
        /// </summary>
        public async UniTask<(LevelData levelData, string levelAttemptId)?> TrySetup(int levelId)
        {
            string startLevelRequestIdempotencyKey = Guid.NewGuid().ToString("N");
            UniTask<LevelData> levelDataTask = TryLoadLevelData(levelId);
            UniTask<string> levelAttemptIdTask = TryStartLevelAttempt(startLevelRequestIdempotencyKey, levelId);
            var (levelData, levelAttemptId) = await UniTask.WhenAll(levelDataTask, levelAttemptIdTask);
            if (levelData == null || levelAttemptId == null) return null;
            return (levelData, levelAttemptId);
        }

        /// <summary>
        /// [Duong] Tries to get level config data
        /// </summary>
        private async UniTask<LevelData> TryLoadLevelData(int levelId)
        {
            try
            {
                return await configManager.GetLevelDataAsync(levelId);
            }
            catch (IOException exception)
            {
                Debug.LogWarning(exception);
                await RunInformationDialog("Level unavailable", "This level is currently unavailable. Please try again later.");
                return null;
            }
        }

        /// <summary>
        /// [Duong] Asks server to start a new level attempt.
        /// </summary>
        private async UniTask<string> TryStartLevelAttempt(string startLevelRequestIdempotencyKey, int levelId)
        {
            while (true)
            {
                StartLevelAttemptResponse response;
                try
                {
                    response = await levelAttemptService.StartLevelAttempt(PlayerAccountContext.Instance.CurrentAccount.AuthSession, startLevelRequestIdempotencyKey, levelId);
                }
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (await RunRetryDecisionDialog(LevelAttemptDialogText.RetryTitle, LevelAttemptDialogText.StartRetryMessage)) continue;
                    return null;
                }
                catch (PlayFabRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await RunInformationDialog(LevelAttemptDialogText.RejectionTitle, LevelAttemptDialogText.StartRetryMessage);
                    return null;
                }

                playerLivesPresentationService.ReplaceServerLivesSnapshot(response.lives);
                if (response.outcome == StartLevelAttemptOutcome.Approved) return response.levelAttemptId;

                await RunInformationDialog(LevelAttemptDialogText.RejectionTitle, LevelAttemptDialogText.GetStartRejectionMessage(response.rejectionReason.Value));
                return null;
            }
        }
        
        private  async UniTask<bool> RunRetryDecisionDialog(string title, string message)
        {
            bool shouldRetry = false;
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow(title, message, async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                switch ((DialogButtonType)buttonId)
                {
                    case DialogButtonType.Cancel:
                        return;
                    case DialogButtonType.Retry:
                        shouldRetry = true;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported retry-decision dialog button.");
                }
            }, options);
            return shouldRetry;
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
    }
}
