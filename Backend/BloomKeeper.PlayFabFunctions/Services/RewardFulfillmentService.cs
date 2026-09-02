using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// [Duong] Grants the reward based on its kind.
/// </summary>
public class RewardFulfillmentService
{
    private readonly PlayFabFunctionContextReader contextReader;
    private readonly PlayFabInventoryService inventoryService;

    public RewardFulfillmentService(PlayFabFunctionContextReader contextReader, PlayFabInventoryService inventoryService)
    {
        this.contextReader = contextReader ?? throw new ArgumentNullException(nameof(contextReader));
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }
    
    /// <summary>
    /// [Duong] Grand a reward to player
    /// </summary>
    public async Task<RewardFulfillmentResult> FulfillReward(RewardGrant reward, PlayFabFunctionExecutionContext context, string rewardGrantIdempotencyKey)
    {
        if (reward == null) throw new ArgumentNullException(nameof(reward));
        if (context == null) throw new ArgumentNullException(nameof(context));

        switch (reward.kind)
        {
            case RewardKind.InventoryItem:
                return await FulfillInventoryItemReward(reward, context, rewardGrantIdempotencyKey);
            case RewardKind.Currency:
                throw new NotSupportedException("Currency reward fulfillment is not implemented.");
            default:
                throw new ArgumentOutOfRangeException(nameof(reward.kind), reward.kind, "Unsupported reward kind.");
        }
    }

    /// <summary>
    /// [Duong] Grand a item reward to player inventory
    /// </summary>
    private async Task<RewardFulfillmentResult> FulfillInventoryItemReward(RewardGrant reward, PlayFabFunctionExecutionContext context, string rewardGrantIdempotencyKey)
    {
        if (reward.inventoryItem == null) throw new ArgumentException("Inventory item reward has no inventory item payload.", nameof(reward));

        var economyApi = contextReader.CreateEconomyApi(context);
        var callerEntity = contextReader.GetCallerEconomyEntity(context);
        await inventoryService.AddInventoryItem(economyApi, callerEntity, reward.inventoryItem.itemCatalogId, reward.inventoryItem.quantity, rewardGrantIdempotencyKey);
        return new InventoryItemRewardFulfillmentResult(reward);
    }
}
