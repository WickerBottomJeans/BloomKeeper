using DefaultNamespace;
using PlayFab;
using PlayFab.EconomyModels;
using PlayFab.Internal;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabBoosterInventoryStore
{
    private const string BoosterContentType = "booster";
    private const string FriendlyIdAlternateIdType = "FriendlyId";
    private const int InventoryPageSize = 50;
    private static readonly IReadOnlyList<string> SupportedFriendlyIds = new[] { BoosterCatalogIds.BloomWandFriendlyId, BoosterCatalogIds.GardenersGloveFriendlyId };

    public async Task<Dictionary<string, int>> LoadInventory(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));

        Dictionary<string, string> friendlyIdsByCatalogId = await ResolveCatalogIds(economyApi);
        Dictionary<string, int> quantitiesByFriendlyId = SupportedFriendlyIds.ToDictionary(friendlyId => friendlyId, _ => 0);
        var observedFriendlyIds = new HashSet<string>();
        string continuationToken = null;
        var observedContinuationTokens = new HashSet<string>();

        do
        {
            var request = new GetInventoryItemsRequest { Count = InventoryPageSize, ContinuationToken = continuationToken, Entity = callerEntity };
            PlayFabResult<GetInventoryItemsResponse> result = await economyApi.GetInventoryItemsAsync(request);
            GetInventoryItemsResponse response = GetRequiredPlayFabResult(result, "GetInventoryItems");
            if (response.Items == null) throw new InvalidOperationException("PlayFab GetInventoryItems returned no inventory item collection.");

            foreach (InventoryItem item in response.Items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) throw new InvalidOperationException("PlayFab GetInventoryItems returned an inventory item without a catalog ID.");
                if (!friendlyIdsByCatalogId.TryGetValue(item.Id, out string friendlyId)) continue;
                if (!string.Equals(item.StackId, BoosterInventoryContract.CanonicalStackId, StringComparison.Ordinal)) throw new InvalidOperationException($"PlayFab booster {friendlyId} uses unsupported stack {item.StackId}.");
                if (!item.Amount.HasValue || item.Amount.Value < 0) throw new InvalidOperationException($"PlayFab GetInventoryItems returned an invalid amount for {friendlyId}.");
                if (!observedFriendlyIds.Add(friendlyId)) throw new InvalidOperationException($"PlayFab GetInventoryItems returned duplicate canonical stacks for {friendlyId}.");
                quantitiesByFriendlyId[friendlyId] = item.Amount.Value;
            }

            continuationToken = response.ContinuationToken;
            if (!string.IsNullOrEmpty(continuationToken) && !observedContinuationTokens.Add(continuationToken))
                throw new InvalidOperationException("PlayFab GetInventoryItems repeated a continuation token.");
        } while (!string.IsNullOrEmpty(continuationToken));

        return quantitiesByFriendlyId;
    }

    public async Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, Dictionary<string, int> quantities)> ConsumeOne(PlayFabEconomyInstanceAPI economyApi, EntityKey callerEntity, string boosterFriendlyId, string operationId)
    {
        if (economyApi == null) throw new ArgumentNullException(nameof(economyApi));
        if (callerEntity == null) throw new ArgumentNullException(nameof(callerEntity));
        if (!SupportedFriendlyIds.Contains(boosterFriendlyId)) throw new ArgumentOutOfRangeException(nameof(boosterFriendlyId), boosterFriendlyId, "Booster Friendly ID is not supported.");

        Dictionary<string, string> friendlyIdsByCatalogId = await ResolveCatalogIds(economyApi);
        string catalogId = friendlyIdsByCatalogId.Single(entry => entry.Value == boosterFriendlyId).Key;
        var request = new SubtractInventoryItemsRequest
        {
            Amount = 1,
            DeleteEmptyStacks = false,
            Entity = callerEntity,
            IdempotencyId = operationId,
            Item = new InventoryItemReference { Id = catalogId, StackId = BoosterInventoryContract.CanonicalStackId }
        };
        PlayFabResult<SubtractInventoryItemsResponse> result = await economyApi.SubtractInventoryItemsAsync(request);
        if (result == null) throw new InvalidOperationException("PlayFab SubtractInventoryItems returned no result.");

        if (result.Error != null)
        {
            if (result.Error.Error != PlayFabErrorCode.InsufficientFunds) throw new InvalidOperationException($"PlayFab SubtractInventoryItems failed: {result.Error.GenerateErrorReport()}");
            Dictionary<string, int> rejectedQuantities = await LoadInventory(economyApi, callerEntity);
            return (ConsumeBoosterOutcome.Rejected, ConsumeBoosterRejectionReason.InsufficientQuantity, rejectedQuantities);
        }

        if (result.Result == null) throw new InvalidOperationException("PlayFab SubtractInventoryItems returned no response body.");
        Dictionary<string, int> quantities = await LoadInventory(economyApi, callerEntity);
        return (ConsumeBoosterOutcome.Consumed, null, quantities);
    }

    /// <summary>
    /// [Duong] Builds a map from each PlayFab catalog ID to its booster Friendly ID
    /// </summary>
    /// <param name="economyApi"></param>
    /// <returns>Catalog ID → Friendly ID dict.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<Dictionary<string, string>> ResolveCatalogIds(PlayFabEconomyInstanceAPI economyApi)
    {
        // [Duong] Fetch the supported booster catalog items from PlayFab by Friendly ID
        var request = new GetItemsRequest
        {
            AlternateIds = SupportedFriendlyIds.Select(friendlyId => new CatalogAlternateId { Type = FriendlyIdAlternateIdType, Value = friendlyId }).ToList()
        };
        PlayFabResult<GetItemsResponse> result = await economyApi.GetItemsAsync(request);
        GetItemsResponse boosterItems = GetRequiredPlayFabResult(result, "GetItems");
        if (boosterItems.Items == null) throw new InvalidOperationException("PlayFab GetItems returned no catalog item collection.");

        // Build a catalog ID to Friendly ID lookup.
        var friendlyIdsByCatalogId = new Dictionary<string, string>();
        foreach (CatalogItem item in boosterItems.Items)
        {
            // Validate the resolved catalog item.
            if (item == null || string.IsNullOrWhiteSpace(item.Id)) throw new InvalidOperationException("PlayFab GetItems returned a catalog item without an ID.");
            if (!string.Equals(item.ContentType, BoosterContentType, StringComparison.Ordinal))
                throw new InvalidOperationException($"PlayFab catalog item {item.Id} is not a {BoosterContentType}.");

            CatalogAlternateId friendlyId = item.AlternateIds?.SingleOrDefault(alternateId => alternateId.Type == FriendlyIdAlternateIdType);
            if (friendlyId == null || !SupportedFriendlyIds.Contains(friendlyId.Value))
                throw new InvalidOperationException($"PlayFab catalog item {item.Id} has an unsupported Friendly ID.");
            if (!friendlyIdsByCatalogId.TryAdd(item.Id, friendlyId.Value))
                throw new InvalidOperationException($"PlayFab GetItems returned duplicate catalog ID {item.Id}.");
        }

        // Require every supported booster to resolve successfully.
        if (friendlyIdsByCatalogId.Count != SupportedFriendlyIds.Count)
            throw new InvalidOperationException("PlayFab GetItems did not resolve every supported booster Friendly ID.");

        // Return the mapping used by inventory operations.
        return friendlyIdsByCatalogId;
    }

    private static T GetRequiredPlayFabResult<T>(PlayFabResult<T> result, string operationName) where T : PlayFabResultCommon
    {
        if (result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no result.");
        if (result.Error != null) throw new InvalidOperationException($"PlayFab {operationName} failed: {result.Error.GenerateErrorReport()}");
        return result.Result ?? throw new InvalidOperationException($"PlayFab {operationName} returned no response body.");
    }
}
