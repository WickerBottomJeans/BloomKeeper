using DefaultNamespace;
using PlayFab;
using PlayFab.EconomyModels;
using PlayFab.Internal;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// [Duong] Reads and changes PlayFab inventory.
/// </summary>
public class PlayFabInventoryService
{
    private const int InventoryPageSize = 50;

    /// <summary>
    /// Load player's playfab inventory
    /// </summary>
    public async Task<IReadOnlyList<InventoryItem>> LoadPlayerInventoryItems(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));

        var inventoryItems = new List<InventoryItem>();
        var observedContinuationTokens = new HashSet<string>();
        string continuationToken = null;
        // Read every page of player inventory.
        do
        {
            var request = new GetInventoryItemsRequest { Count = InventoryPageSize, ContinuationToken = continuationToken, Entity = callerEntity };
            PlayFabResult<GetInventoryItemsResponse> result = await economyApi.GetInventoryItemsAsync(request);
            GetInventoryItemsResponse response = GetRequiredPlayFabResult(result, "GetInventoryItems");
            if (response.Items == null) throw new InvalidOperationException("PlayFab GetInventoryItems returned a null inventory item collection.");
            inventoryItems.AddRange(response.Items);

            continuationToken = response.ContinuationToken;
            if (!string.IsNullOrEmpty(continuationToken) && !observedContinuationTokens.Add(continuationToken)) throw new InvalidOperationException("PlayFab GetInventoryItems repeated a continuation token.");
        } while (!string.IsNullOrEmpty(continuationToken));

        return inventoryItems.AsReadOnly();
    }

    /// <summary>
    /// Creates a sparse inventory snapshot from PlayFab inventory items.
    /// </summary>
    public PlayerInventorySnapshot CreatePlayerInventorySnapshot(IReadOnlyList<InventoryItem> playerInventoryItems)
    {
        if (playerInventoryItems == null) throw new ArgumentNullException(nameof(playerInventoryItems));

        var quantitiesByCatalogId = new Dictionary<string, int>();
        foreach (InventoryItem item in playerInventoryItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id)) throw new InvalidOperationException("PlayFab inventory contains an item without a catalog ID.");
            if (!string.Equals(item.StackId, PlayerInventoryContract.CanonicalStackId)) throw new InvalidOperationException($"PlayFab inventory item {item.Id} uses unsupported stack {item.StackId}.");
            if (!item.Amount.HasValue || item.Amount.Value < 0) throw new InvalidOperationException($"PlayFab inventory item {item.Id} has an invalid amount.");
            if (!quantitiesByCatalogId.TryAdd(item.Id, item.Amount.Value)) throw new InvalidOperationException($"PlayFab inventory contains duplicate canonical stacks for {item.Id}.");
        }

        return new PlayerInventorySnapshot { quantitiesByCatalogId = quantitiesByCatalogId };
    }

    /// <summary>
    /// Adds an amount of one inventory item.
    /// </summary>
    public async Task AddInventoryItem(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemCatalogId, int amount, string inventoryMutationIdempotencyKey)
    {
        ValidateInventoryOperation(economyApi, callerEntity, itemCatalogId, amount, inventoryMutationIdempotencyKey);

        var request = new AddInventoryItemsRequest
        {
            Amount = amount,
            Entity = callerEntity,
            IdempotencyId = inventoryMutationIdempotencyKey,
            Item = CreateInventoryItemReference(itemCatalogId)
        };
        PlayFabResult<AddInventoryItemsResponse> result = await economyApi.AddInventoryItemsAsync(request);
        GetRequiredPlayFabResult(result, "AddInventoryItems");
    }

    /// <summary>
    /// [Duong] Subtracts an inventory item, or returns false when there is not enough.
    /// </summary>
    public async Task<bool> TrySubtractInventoryItem(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemCatalogId, int amount, string inventoryMutationIdempotencyKey)
    {
        ValidateInventoryOperation(economyApi, callerEntity, itemCatalogId, amount, inventoryMutationIdempotencyKey);

        var request = new SubtractInventoryItemsRequest
        {
            Amount = amount,
            DeleteEmptyStacks = false,
            Entity = callerEntity,
            IdempotencyId = inventoryMutationIdempotencyKey,
            Item = CreateInventoryItemReference(itemCatalogId)
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

    /// <summary>
    /// Creates a reference to an item's default inventory stack.
    /// </summary>
    private static InventoryItemReference CreateInventoryItemReference(string itemCatalogId)
    {
        return new InventoryItemReference { Id = itemCatalogId, StackId = PlayerInventoryContract.CanonicalStackId };
    }

    /// <summary>
    /// [Duong] Validates one inventory change request.
    /// </summary>
    private static void ValidateInventoryOperation(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string itemCatalogId, int amount, string inventoryMutationIdempotencyKey)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));
        if (string.IsNullOrWhiteSpace(itemCatalogId)) throw new ArgumentException("Inventory item catalog ID is missing.", nameof(itemCatalogId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Inventory item amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(inventoryMutationIdempotencyKey)) throw new ArgumentException("Inventory mutation idempotency key is missing.", nameof(inventoryMutationIdempotencyKey));
    }

    /// <summary>
    /// [Duong] String and int safety checks
    /// </summary>
    private static void ValidateInventoryItem(string itemCatalogId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemCatalogId)) throw new ArgumentException("Inventory item catalog ID is missing.", nameof(itemCatalogId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Inventory item amount must be greater than zero.");
    }

    /// <summary>
    /// [Duong] Returns a successful PlayFab response body or throws its error.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="operationName">Text only; not used in logic</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static T GetRequiredPlayFabResult<T>(PlayFabResult<T> result, string operationName)
        where T : PlayFabResultCommon
    {
        if (result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no result.");
        if (result.Error != null)
            throw new InvalidOperationException(
                $"PlayFab {operationName} failed: {result.Error.GenerateErrorReport()}");
        return result.Result ??
               throw new InvalidOperationException($"PlayFab {operationName} returned no response body.");
    }
}
