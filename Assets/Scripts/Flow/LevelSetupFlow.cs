using System;
using System.IO;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Owns only pre-level setup: loads the level config, obtains server approval, and resolves failures that block entry.
    /// Does not start, run, or finish the local level session.
    /// </summary>
    public class LevelSetupFlow
    {
        private readonly PlayFabLevelAttemptService levelAttemptService;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;

        public LevelSetupFlow(PlayFabLevelAttemptService levelAttemptService, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.levelAttemptService = levelAttemptService ?? throw new ArgumentNullException(nameof(levelAttemptService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// [Duong] Tries to obtain server approval and the config required to set up a level.
        /// </summary>
        public async UniTask<(LevelData levelData, string levelAttemptId)?> TrySetup(int levelId)
        {
            string startLevelRequestIdempotencyKey = Guid.NewGuid().ToString("N");
            StartLevelAttemptResponse response = await TryStartLevelAttempt(startLevelRequestIdempotencyKey, levelId);
            if (response == null) return null;
            try
            {
                LevelData levelData = JsonConvert.DeserializeObject<LevelData>(response.levelJson);
                if (levelData == null || levelData.levelId != levelId || levelData.boardWidth <= 0 || levelData.boardHeight <= 0 || levelData.tiles == null || levelData.tiles.Count != (long)levelData.boardWidth * levelData.boardHeight || levelData.starScoreThresholds == null || levelData.starScoreThresholds.Count == 0 || levelData.objectives == null || levelData.allowedBoosters == null) throw new InvalidDataException($"Level {levelId} has invalid data.");
                return (levelData, response.levelAttemptId);
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException)
            {
                Debug.LogWarning(exception);
                try
                {
                    await levelAttemptService.AbandonLevelAttempt(PlayerAccountContext.Instance.CurrentAccount.AuthSession, response.levelAttemptId);
                }
                catch (PlayFabRequestException abandonException)
                {
                    Debug.LogWarning(abandonException);
                }
                await DialogManager.Instance.RunOkDialog("Level unavailable", "This level is currently unavailable. Please try again later.");
                return null;
            }
        }

        /// <summary>
        /// [Duong] Asks server to start a new level attempt.
        /// </summary>
        private async UniTask<StartLevelAttemptResponse> TryStartLevelAttempt(string startLevelRequestIdempotencyKey, int levelId)
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
                    if (await DialogManager.Instance.RunRetryOrCancelDialog(LevelAttemptDialogText.RetryTitle, LevelAttemptDialogText.StartRetryMessage)) continue;
                    return null;
                }
                catch (PlayFabRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await DialogManager.Instance.RunOkDialog(LevelAttemptDialogText.RejectionTitle, LevelAttemptDialogText.StartRetryMessage);
                    return null;
                }

                playerLivesPresentationService.ReplaceServerLivesSnapshot(response.lives);
                if (response.outcome == StartLevelAttemptOutcome.Approved) return response;

                await DialogManager.Instance.RunOkDialog(LevelAttemptDialogText.RejectionTitle, LevelAttemptDialogText.GetStartRejectionMessage(response.rejectionReason.Value));
                return null;
            }
        }
        
    }
}
