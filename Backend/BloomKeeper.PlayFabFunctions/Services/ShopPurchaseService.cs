using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;
using PlayFab;
using PlayFab.EconomyModels;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// Buys shop offers by exchanging inventory items.
/// </summary>
public class ShopPurchaseService
{
    private readonly PlayFabInventoryService inventoryService = new PlayFabInventoryService();

    /// <summary>
    /// [Duong] Subtracts the cost, adds the grants, and returns the purchase result.
    /// </summary>
    public async Task<BuyShopOfferResponse> BuyShopOffer(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, ShopOfferConfig shopOffer, string buyShopOfferIdempotencyKey)
    {
        if (shopOffer == null) throw new ArgumentNullException(nameof(shopOffer));

        // [Duong] Collect inventory items granted by the offer.
        var grantItems = new List<(string itemCatalogId, int amount)>();
        foreach (ShopGrantConfig grant in shopOffer.grants)
        {
            switch (grant.kind)
            {
                case ShopGrantKind.InventoryItem:
                    grantItems.Add((grant.inventoryItem.itemCatalogId, grant.inventoryItem.quantity));
                    break;
                case ShopGrantKind.UnlimitedLives:
                    // TODO: Use a durable saga for unlimited-lives purchases.
                    throw new InvalidOperationException($"Shop offer {shopOffer.offerId} contains an unsupported unlimited-lives grant.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(grant.kind), grant.kind, "Unsupported shop grant kind.");
            }
        }

        bool purchased = await inventoryService.TryExecuteInventoryItemExchange(economyApi, callerEntity, shopOffer.cost.itemCatalogId, shopOffer.cost.quantity, grantItems, buyShopOfferIdempotencyKey);

        // [Duong] Return updated inventory after the purchase attempt.
        IReadOnlyList<InventoryItem> inventoryItems = await inventoryService.LoadPlayerInventoryItems(economyApi, callerEntity);
        PlayerInventorySnapshot playerInventorySnapshot = inventoryService.CreatePlayerInventorySnapshot(inventoryItems);
        return purchased
            ? new BuyShopOfferResponse { schemaVersion = ShopContract.CurrentSchemaVersion, outcome = BuyShopOfferOutcome.Purchased, playerInventorySnapshot = playerInventorySnapshot }
            : new BuyShopOfferResponse { schemaVersion = ShopContract.CurrentSchemaVersion, outcome = BuyShopOfferOutcome.Rejected, rejectionReason = BuyShopOfferRejectionReason.InsufficientCostItemQuantity, playerInventorySnapshot = playerInventorySnapshot };
    }
}
