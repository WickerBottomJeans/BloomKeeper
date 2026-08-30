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
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (await DialogManager.Instance.RunRetryOrCancelDialog(LevelAttemptDialogText.RetryTitle, LevelAttemptDialogText.AbandonRetryMessage)) continue;
                    return false;
                }
                catch (PlayFabRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await DialogManager.Instance.RunOkDialog(LevelAttemptDialogText.AbandonRejectionTitle, LevelAttemptDialogText.AbandonRetryMessage);
                    return false;
                }

                if (response.outcome == AbandonLevelAttemptOutcome.Abandoned) return true;
                await DialogManager.Instance.RunOkDialog(LevelAttemptDialogText.AbandonRejectionTitle, LevelAttemptDialogText.GetAbandonRejectionMessage(response.rejectionReason.Value));
                return false;
            }
        }

        private  async UniTask<bool> RunQuitConfirmationDialog()
        {
            var quitButton = new DialogOptionButton(DialogButtonType.Yes, "Quit", DialogButtonColorVariant.Orange);
            DialogOptionButton[] options = { DialogOptionButton.Cancel, quitButton };
            DialogButtonType buttonType = await DialogManager.Instance.RunDialog("Quit level?", "Your progress in this level will be lost.", options);
            switch (buttonType)
            {
                case DialogButtonType.Cancel:
                    return false;
                case DialogButtonType.Yes:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttonType), buttonType, "Unsupported quit-level dialog button.");
            }
        }

    }
}
