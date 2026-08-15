using PlayFab;
using PlayFab.EconomyModels;
using PlayFab.Internal;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabInventoryService
{
    private const string FriendlyIdAlternateIdType = "FriendlyId";
    private const string DefaultStackId = "default";

    public async Task AddInventoryItem(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemFriendlyId, int amount, string inventoryMutationIdempotencyKey)
    {
        ValidateInventoryOperation(economyApi, callerEntity, itemFriendlyId, amount, inventoryMutationIdempotencyKey);

        var request = new AddInventoryItemsRequest
        {
            Amount = amount,
            Entity = callerEntity,
            IdempotencyId = inventoryMutationIdempotencyKey,
            Item = CreateInventoryItemReference(itemFriendlyId)
        };
        PlayFabResult<AddInventoryItemsResponse> result = await economyApi.AddInventoryItemsAsync(request);
        GetRequiredPlayFabResult(result, "AddInventoryItems");
    }

    public async Task<bool> TrySubtractInventoryItem(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemFriendlyId, int amount, string inventoryMutationIdempotencyKey)
    {
        ValidateInventoryOperation(economyApi, callerEntity, itemFriendlyId, amount, inventoryMutationIdempotencyKey);

        var request = new SubtractInventoryItemsRequest
        {
            Amount = amount,
            DeleteEmptyStacks = false,
            Entity = callerEntity,
            IdempotencyId = inventoryMutationIdempotencyKey,
            Item = CreateInventoryItemReference(itemFriendlyId)
        };
        PlayFabResult<SubtractInventoryItemsResponse> result = await economyApi.SubtractInventoryItemsAsync(request);
        if (result == null) throw new InvalidOperationException("PlayFab SubtractInventoryItems returned no result.");
        if (result.Error != null)
        {
            if (result.Error.Error == PlayFabErrorCode.InsufficientFunds) return false;
            throw new InvalidOperationException($"PlayFab SubtractInventoryItems failed: {result.Error.GenerateErrorReport()}");
        }

        if (result.Result == null) throw new InvalidOperationException("PlayFab SubtractInventoryItems returned no response body.");
        return true;
    }

    private static InventoryItemReference CreateInventoryItemReference(string itemFriendlyId)
    {
        return new InventoryItemReference { AlternateId = new AlternateId { Type = FriendlyIdAlternateIdType, Value = itemFriendlyId }, StackId = DefaultStackId };
    }

    private static void ValidateInventoryOperation(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemFriendlyId, int amount, string inventoryMutationIdempotencyKey)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));
        if (string.IsNullOrWhiteSpace(itemFriendlyId)) throw new ArgumentException("Inventory item Friendly ID is missing.", nameof(itemFriendlyId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Inventory item amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(inventoryMutationIdempotencyKey)) throw new ArgumentException("Inventory mutation idempotency key is missing.", nameof(inventoryMutationIdempotencyKey));
    }

    private static T GetRequiredPlayFabResult<T>(PlayFabResult<T> result, string operationName) where T : PlayFabResultCommon
    {
        if (result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no result.");
        if (result.Error != null) throw new InvalidOperationException($"PlayFab {operationName} failed: {result.Error.GenerateErrorReport()}");
        return result.Result ?? throw new InvalidOperationException($"PlayFab {operationName} returned no response body.");
    }
}
