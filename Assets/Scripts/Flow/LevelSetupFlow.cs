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

        public LevelSetupFlow(ConfigManager configManager, PlayFabLevelAttemptService levelAttemptService)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
        }

        /// <summary>
        /// [Duong] Tries to obtain server approval and the config required to set up a level.
        /// </summary>
        /// <returns>Level data and server attempt ID, or null when entry does not proceed.</returns>
        public async UniTask<(LevelData levelData, string levelAttemptId)?> TrySetup(int levelId)
        {
            LevelData levelData;
            try
            {
                levelData = await configManager.GetLevelDataAsync(levelId);
            }
            catch (IOException exception)
            {
                Debug.LogWarning(exception);
                await RunInformationDialog("Level unavailable", "This level is currently unavailable. Please try again later.");
                return null;
            }

            string startOperationId = Guid.NewGuid().ToString("N");
            while (true)
            {
                StartLevelAttemptResponse response;
                try
                {
                    response = await levelAttemptService.StartLevelAttempt(PlayerAccountContext.Instance.CurrentAccount.AuthSession, startOperationId, levelId);
                }
                catch (LevelAttemptRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (await RunRetryDecisionDialog(LevelAttemptDialogText.RetryTitle, LevelAttemptDialogText.StartRetryMessage)) continue;
                    return null;
                }
                catch (LevelAttemptRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await RunInformationDialog(LevelAttemptDialogText.RejectionTitle, LevelAttemptDialogText.StartRetryMessage);
                    return null;
                }

                if (response.outcome == StartLevelAttemptOutcome.Approved)
                    return (levelData, response.levelAttemptId);

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
