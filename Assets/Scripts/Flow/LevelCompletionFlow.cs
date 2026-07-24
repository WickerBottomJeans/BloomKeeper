using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class LevelCompletionFlow
    {
        private readonly PlayFabProgressionService progressionService;

        public LevelCompletionFlow(PlayFabProgressionService progressionService)
        {
            this.progressionService = progressionService;
        }

        public async UniTask<CompleteLevelAttemptResponse> Enter(LevelSessionResult result)
        {
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            CompleteLevelAttemptResponse response = await ApplicationPresentationService.Instance.RunWithLoading(() => progressionService.CompleteLevelAttempt(account.AuthSession, CreateCompleteLevelAttemptRequest(result)));
            if (response.outcome == CompleteLevelAttemptOutcome.Saved)
                ApplyCompleteLevelAttemptResponse(response);
            return response;
        }

        public void Exit()
        {
        }

        /// <summary>
        /// Convert local result into a DTO so the server can check if it's legit
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static CompleteLevelAttemptRequest CreateCompleteLevelAttemptRequest(LevelSessionResult result)
        {
            return new CompleteLevelAttemptRequest { attemptId = result.AttemptId, levelId = result.LevelId, didWin = result.DidWin, score = result.Score, stars = result.Stars };
        }

        private static void ApplyCompleteLevelAttemptResponse(CompleteLevelAttemptResponse response)
        {
            PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
            progression.highestUnlockedLevel = response.highestUnlockedLevel;
            progression.levels[response.levelId] = response.levelProgress;
        }
    }
}
