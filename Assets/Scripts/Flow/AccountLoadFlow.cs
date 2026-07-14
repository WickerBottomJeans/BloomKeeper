using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class AccountLoadFlow
    {
        private readonly PlayFabProgressionService progressionService;

        public AccountLoadFlow(PlayFabProgressionService progressionService)
        {
            this.progressionService = progressionService;
        }

        public async UniTask<PlayerAccount> Enter(PlayFabAuthSession authSession)
        {
            PlayerProgressionData progression = await progressionService.LoadProgression(authSession);
            return new PlayerAccount(authSession, progression);
        }

        public void Exit()
        {
        }
    }
}
