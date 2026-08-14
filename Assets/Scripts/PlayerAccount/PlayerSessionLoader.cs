using System.Threading.Tasks;

namespace DefaultNamespace
{
    public class PlayerSessionLoader
    {
        private readonly PlayFabPlayerStateService playerStateService;
        private readonly PlayFabBoosterInventoryService boosterInventoryService;

        public PlayerSessionLoader(PlayFabPlayerStateService playerStateService, PlayFabBoosterInventoryService boosterInventoryService)
        {
            this.playerStateService = playerStateService;
            this.boosterInventoryService = boosterInventoryService;
        }

        public async Task<(PlayerAccount account, PlayerLivesSnapshot livesSnapshot)> Load(PlayFabAuthSession authSession)
        {
            Task<LoadPlayerStateResponse> playerStateTask = playerStateService.LoadPlayerState(authSession);
            Task<BoosterInventoryData> boosterInventoryTask = boosterInventoryService.LoadInventory(authSession);

            LoadPlayerStateResponse playerState = await playerStateTask;
            BoosterInventoryData boosterInventory = await boosterInventoryTask;
            var account = new PlayerAccount(authSession, playerState.progression, boosterInventory);
            return (account, playerState.lives);
        }
    }
}
