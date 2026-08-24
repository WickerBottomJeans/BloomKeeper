using System.Threading.Tasks;

namespace DefaultNamespace
{
    public class PlayerSessionLoader
    {
        private readonly PlayFabPlayerStateService playerStateService;
        private readonly PlayFabInventoryService inventoryService;

        public PlayerSessionLoader(PlayFabPlayerStateService playerStateService, PlayFabInventoryService inventoryService)
        {
            this.playerStateService = playerStateService;
            this.inventoryService = inventoryService;
        }

        public async Task<(PlayerAccount account, PlayerLivesSnapshot livesSnapshot)> Load(PlayFabAuthSession authSession)
        {
            Task<LoadPlayerStateResponse> playerStateTask = playerStateService.LoadPlayerState(authSession);
            Task<PlayerInventoryData> playerInventoryTask = inventoryService.LoadPlayerInventory(authSession);

            LoadPlayerStateResponse playerState = await playerStateTask;
            PlayerInventoryData playerInventory = await playerInventoryTask;
            var account = new PlayerAccount(authSession, playerState.progression, playerInventory);
            return (account, playerState.lives);
        }
    }
}
