using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace DefaultNamespace
{
    public class AccountLoadFlow
    {
        private readonly PlayFabProgressionService progressionService;
        private readonly PlayFabBoosterInventoryService boosterInventoryService;

        public AccountLoadFlow(PlayFabProgressionService progressionService, PlayFabBoosterInventoryService boosterInventoryService)
        {
            this.progressionService = progressionService;
            this.boosterInventoryService = boosterInventoryService;
        }

        public async UniTask<PlayerAccount> Enter(PlayFabAuthSession authSession)
        {
            Task<PlayerProgressionData> progressionTask = progressionService.LoadProgression(authSession);
            Task<BoosterInventoryData> boosterInventoryTask = boosterInventoryService.LoadInventory(authSession);

            PlayerProgressionData progression = await progressionTask;
            BoosterInventoryData boosterInventory = await boosterInventoryTask;
            return new PlayerAccount(authSession, progression, boosterInventory);
        }

        public void Exit()
        {
        }
    }
}
