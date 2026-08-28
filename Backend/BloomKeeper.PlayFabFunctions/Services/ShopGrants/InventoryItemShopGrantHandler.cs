using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services.ShopGrants;

/// <summary>
/// Adds inventory items from one shop grant.
/// </summary>
public class InventoryItemShopGrantHandler : IShopGrantHandler
{
    private readonly PlayFabFunctionContextReader contextReader;
    private readonly PlayFabInventoryService inventoryService;

    public ShopGrantKind GrantKind => ShopGrantKind.InventoryItem;

    /// <summary>
    /// Creates the handler with its PlayFab inventory dependencies.
    /// </summary>
    public InventoryItemShopGrantHandler(PlayFabFunctionContextReader contextReader, PlayFabInventoryService inventoryService)
    {
        this.contextReader = contextReader ?? throw new ArgumentNullException(nameof(contextReader));
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    /// <summary>
    /// Adds the configured item quantity to the player's inventory.
    /// </summary>
    public async Task ApplyShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext context, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (shopGrantConfig.inventoryItem == null) throw new InvalidOperationException($"Shop grant {shopGrantConfig.grantId} has no inventory item payload.");

        cancellationToken.ThrowIfCancellationRequested();
        var economyApi = contextReader.CreateEconomyApi(context);
        var economyEntity = contextReader.GetCallerEconomyEntity(context);
        await inventoryService.AddInventoryItem(economyApi, economyEntity, shopGrantConfig.inventoryItem.itemCatalogId, shopGrantConfig.inventoryItem.quantity, shopGrantIdempotencyKey);
    }

    /// <summary>
    /// Removes the configured item quantity from the player's inventory.
    /// </summary>
    public async Task RevertShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext playFabFunctionExecutionContext, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (shopGrantConfig.inventoryItem == null) throw new InvalidOperationException($"Shop grant {shopGrantConfig.grantId} has no inventory item payload.");

        cancellationToken.ThrowIfCancellationRequested();
        var playFabEconomyApi = contextReader.CreateEconomyApi(playFabFunctionExecutionContext);
        var callerEconomyEntity = contextReader.GetCallerEconomyEntity(playFabFunctionExecutionContext);
        string shopGrantRevertIdempotencyKey = $"{shopGrantIdempotencyKey}-revert";
        bool shopGrantWasReverted = await inventoryService.TrySubtractInventoryItem(playFabEconomyApi, callerEconomyEntity, shopGrantConfig.inventoryItem.itemCatalogId, shopGrantConfig.inventoryItem.quantity, shopGrantRevertIdempotencyKey);
        if (!shopGrantWasReverted) throw new InvalidOperationException($"Shop grant {shopGrantConfig.grantId} could not remove its granted inventory quantity.");
    }
}
