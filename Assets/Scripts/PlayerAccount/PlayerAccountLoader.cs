using System.Threading.Tasks;

namespace DefaultNamespace
{
    public class PlayerAccountLoader
    {
        private readonly PlayFabProgressionService progressionService;
        private readonly PlayFabBoosterInventoryService boosterInventoryService;

        public PlayerAccountLoader(PlayFabProgressionService progressionService, PlayFabBoosterInventoryService boosterInventoryService)
        {
            this.progressionService = progressionService;
            this.boosterInventoryService = boosterInventoryService;
        }

        public async Task<PlayerAccount> Load(PlayFabAuthSession authSession)
        {
            Task<PlayerProgressionData> progressionTask = progressionService.LoadProgression(authSession);
            Task<BoosterInventoryData> boosterInventoryTask = boosterInventoryService.LoadInventory(authSession);

            PlayerProgressionData progression = await progressionTask;
            BoosterInventoryData boosterInventory = await boosterInventoryTask;
            return new PlayerAccount(authSession, progression, boosterInventory);
        }
    }
}
