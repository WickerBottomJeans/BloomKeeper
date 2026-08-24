using DefaultNamespace;
using PlayFab;
using PlayFab.EconomyModels;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabBoosterInventoryStore
{
    private static readonly IReadOnlyList<string> SupportedCatalogIds = new[] { PlayerInventoryCatalogIds.BloomWandCatalogId, PlayerInventoryCatalogIds.GardenersGloveCatalogId };
    private readonly PlayFabInventoryService inventoryService;

    public PlayFabBoosterInventoryStore(PlayFabInventoryService inventoryService)
    {
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    public async Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventorySnapshot playerInventorySnapshot)> ConsumeOne(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string boosterCatalogId, string boosterConsumptionIdempotencyKey)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));
        if (!SupportedCatalogIds.Contains(boosterCatalogId)) throw new ArgumentOutOfRangeException(nameof(boosterCatalogId), boosterCatalogId, "Booster catalog ID is not supported.");

        bool wasSubtracted = await inventoryService.TrySubtractInventoryItem(economyApi, callerEntity, boosterCatalogId, 1, boosterConsumptionIdempotencyKey);
        if (!wasSubtracted)
        {
            IReadOnlyList<InventoryItem> rejectedInventoryItems = await inventoryService.LoadPlayerInventoryItems(economyApi, callerEntity);
            return (ConsumeBoosterOutcome.Rejected, ConsumeBoosterRejectionReason.InsufficientQuantity, inventoryService.CreatePlayerInventorySnapshot(rejectedInventoryItems));
        }

        IReadOnlyList<InventoryItem> inventoryItems = await inventoryService.LoadPlayerInventoryItems(economyApi, callerEntity);
        return (ConsumeBoosterOutcome.Consumed, null, inventoryService.CreatePlayerInventorySnapshot(inventoryItems));
    }
}
