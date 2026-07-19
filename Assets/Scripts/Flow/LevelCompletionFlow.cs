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

        public async UniTask Enter(LevelSessionResult result)
        {
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            CompleteLevelAttemptResponse response = await ApplicationPresentationService.Instance.RunWithLoading(() => progressionService.CompleteLevelAttempt(account.AuthSession, CreateCompleteLevelAttemptRequest(result)));
            ApplyCompleteLevelAttemptResponse(response);
        }

        public void Exit()
        {
        }

        private static CompleteLevelAttemptRequest CreateCompleteLevelAttemptRequest(LevelSessionResult result)
        {
            return new CompleteLevelAttemptRequest { levelId = result.LevelId, didWin = result.DidWin, score = result.Score, stars = result.Stars };
        }

        private static void ApplyCompleteLevelAttemptResponse(CompleteLevelAttemptResponse response)
        {
            PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
            progression.highestUnlockedLevel = response.highestUnlockedLevel;
            progression.levels[response.levelId] = response.levelProgress;
        }
    }
}
