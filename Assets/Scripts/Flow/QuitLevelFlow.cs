using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Confirms and then ask server to abandon level attempt
    /// </summary>
    public class QuitLevelFlow
    {
        private readonly PlayFabLevelAttemptService levelAttemptService;

        public QuitLevelFlow(PlayFabLevelAttemptService levelAttemptService)
        {
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
        }

        /// <summary>
        /// [Duong] Tries to confirm and abandon the active level.
        /// </summary>
        /// <returns>True when the server abandoned the level; otherwise false.</returns>
        public async UniTask<bool> TryQuit(string levelAttemptId)
        {
            if (!Guid.TryParseExact(levelAttemptId, "N", out _)) throw new ArgumentException("Level attempt ID must be a canonical GUID.", nameof(levelAttemptId));
            if (!await RunQuitConfirmationDialog()) return false;
            return await TryAbandonLevelAttempt(levelAttemptId);
        }

        /// <summary>
        /// [Duong] Ask server to abandon this level attempt
        /// </summary>
        private async UniTask<bool> TryAbandonLevelAttempt(string levelAttemptId)
        {
            while (true)
            {
                AbandonLevelAttemptResponse response;
                try
                {
                    PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
                    response = await ApplicationPresentationService.Instance.RunWithLoading(() => levelAttemptService.AbandonLevelAttempt(account.AuthSession, levelAttemptId));
                }
                catch (LevelAttemptRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (await RunRetryDecisionDialog(LevelAttemptDialogText.RetryTitle, LevelAttemptDialogText.AbandonRetryMessage)) continue;
                    return false;
                }
                catch (LevelAttemptRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await RunInformationDialog(LevelAttemptDialogText.AbandonRejectionTitle, LevelAttemptDialogText.AbandonRetryMessage);
                    return false;
                }

                if (response.outcome == AbandonLevelAttemptOutcome.Abandoned) return true;
                await RunInformationDialog(LevelAttemptDialogText.AbandonRejectionTitle, LevelAttemptDialogText.GetAbandonRejectionMessage(response.rejectionReason.Value));
                return false;
            }
        }

        private  async UniTask<bool> RunQuitConfirmationDialog()
        {
            bool quitConfirmed = false;
            var quitButton = new DialogOptionButton(DialogButtonType.Yes, "Quit", DialogButtonVariant.Orange);
            DialogOptionButton[] options = { DialogOptionButton.Cancel, quitButton };
            await DialogManager.Instance.RunDialogWorkflow("Quit level?", "Your progress in this level will be lost.", async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                switch ((DialogButtonType)buttonId)
                {
                    case DialogButtonType.Cancel:
                        return;
                    case DialogButtonType.Yes:
                        quitConfirmed = true;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported quit-level dialog button.");
                }
            }, options);
            return quitConfirmed;
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
