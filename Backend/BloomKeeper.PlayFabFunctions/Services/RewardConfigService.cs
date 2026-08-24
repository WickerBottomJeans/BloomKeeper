using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Services;

public class RewardConfigService
{
    private const string RemoteConfigBaseUrlEnvironmentVariable = "REMOTE_CONFIG_BASE_URL";
    private const string CompletionRewardConfigPath = "rewards/completion.json";
    private const string CompletionRewardTableId = "completion";
    private const int MaximumAwardChanceBasisPoints = 10000;
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly Uri remoteConfigBaseUri;

    public RewardConfigService()
    {
        string remoteConfigBaseUrl = Environment.GetEnvironmentVariable(RemoteConfigBaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(remoteConfigBaseUrl)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} is missing.");
        if (!Uri.TryCreate(remoteConfigBaseUrl, UriKind.Absolute, out Uri parsedRemoteConfigBaseUri)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} must be an absolute URL.");
        remoteConfigBaseUri = parsedRemoteConfigBaseUri.AbsoluteUri.EndsWith('/') ? parsedRemoteConfigBaseUri : new Uri($"{parsedRemoteConfigBaseUri.AbsoluteUri}/");
    }

    public async Task<RewardTableConfig> LoadCompletionRewardTable()
    {
        Uri configUri = new Uri(remoteConfigBaseUri, CompletionRewardConfigPath);
        using HttpResponseMessage response = await HttpClient.GetAsync(configUri);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Failed to load completion reward config from {configUri}. HTTP status: {(int)response.StatusCode} {response.StatusCode}.");

        string json = await response.Content.ReadAsStringAsync();
        RewardTableConfig config;
        try
        {
            config = JsonConvert.DeserializeObject<RewardTableConfig>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Completion reward config at {configUri} contains invalid JSON.", exception);
        }

        ValidateCompletionRewardTable(config);
        return config;
    }

    private static void ValidateCompletionRewardTable(RewardTableConfig config)
    {
        if (config == null) throw new InvalidOperationException("Completion reward config contains invalid JSON.");
        if (config.schemaVersion != RewardContract.CurrentSchemaVersion) throw new InvalidOperationException($"Completion reward config schema version {config.schemaVersion} is unsupported. Expected {RewardContract.CurrentSchemaVersion}.");
        if (!string.Equals(config.tableId, CompletionRewardTableId)) throw new InvalidOperationException($"Completion reward config table ID must be {CompletionRewardTableId}.");
        if (string.IsNullOrWhiteSpace(config.revision)) throw new InvalidOperationException("Completion reward config revision is missing.");
        if (config.awardChanceBasisPoints < 0 || config.awardChanceBasisPoints > MaximumAwardChanceBasisPoints) throw new InvalidOperationException($"Completion reward config award chance must be between 0 and {MaximumAwardChanceBasisPoints} basis points.");
        if (config.entries == null || config.entries.Count == 0) throw new InvalidOperationException("Completion reward config must contain at least one weighted entry.");

        var observedRewardIds = new HashSet<string>();
        int totalWeight = 0;
        foreach (WeightedRewardEntry entry in config.entries)
        {
            if (entry == null) throw new InvalidOperationException("Completion reward config contains a null weighted entry.");
            if (entry.weight <= 0) throw new InvalidOperationException("Completion reward config weights must be greater than zero.");
            if (entry.weight > int.MaxValue - totalWeight) throw new InvalidOperationException("Completion reward config total weight exceeds the supported range.");
            totalWeight += entry.weight;
            ValidateRewardGrant(entry.grant, observedRewardIds);
        }
    }

    private static void ValidateRewardGrant(RewardGrant grant, HashSet<string> observedRewardIds)
    {
        if (grant == null) throw new InvalidOperationException("Completion reward config contains a weighted entry without a grant.");
        if (string.IsNullOrWhiteSpace(grant.rewardId)) throw new InvalidOperationException("Completion reward config contains a grant without a reward ID.");
        if (!observedRewardIds.Add(grant.rewardId)) throw new InvalidOperationException($"Completion reward config contains duplicate reward ID {grant.rewardId}.");
        if (!Enum.IsDefined(typeof(RewardKind), grant.kind)) throw new InvalidOperationException($"Completion reward config contains unsupported reward kind {grant.kind}.");
        if (string.IsNullOrWhiteSpace(grant.presentationKey)) throw new InvalidOperationException($"Completion reward {grant.rewardId} has no presentation key.");

        int payloadCount = (grant.inventoryItem != null ? 1 : 0) + (grant.currency != null ? 1 : 0) + (grant.timedEntitlement != null ? 1 : 0);
        if (payloadCount != 1) throw new InvalidOperationException($"Completion reward {grant.rewardId} must contain exactly one grant payload.");

        switch (grant.kind)
        {
            case RewardKind.InventoryItem:
                ValidateInventoryItemRewardGrant(grant);
                break;
            case RewardKind.Currency:
                ValidateCurrencyRewardGrant(grant);
                break;
            case RewardKind.TimedEntitlement:
                ValidateTimedEntitlementRewardGrant(grant);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(grant.kind), grant.kind, "Unsupported reward kind.");
        }
    }

    private static void ValidateInventoryItemRewardGrant(RewardGrant grant)
    {
        if (grant.inventoryItem == null) throw new InvalidOperationException($"Completion reward {grant.rewardId} kind does not match its payload.");
        if (string.IsNullOrWhiteSpace(grant.inventoryItem.itemCatalogId)) throw new InvalidOperationException($"Completion reward {grant.rewardId} has no inventory item catalog ID.");
        if (grant.inventoryItem.quantity <= 0) throw new InvalidOperationException($"Completion reward {grant.rewardId} inventory quantity must be greater than zero.");
    }

    private static void ValidateCurrencyRewardGrant(RewardGrant grant)
    {
        if (grant.currency == null) throw new InvalidOperationException($"Completion reward {grant.rewardId} kind does not match its payload.");
        if (string.IsNullOrWhiteSpace(grant.currency.currencyId)) throw new InvalidOperationException($"Completion reward {grant.rewardId} has no currency ID.");
        if (grant.currency.amount <= 0) throw new InvalidOperationException($"Completion reward {grant.rewardId} currency amount must be greater than zero.");
    }

    private static void ValidateTimedEntitlementRewardGrant(RewardGrant grant)
    {
        if (grant.timedEntitlement == null) throw new InvalidOperationException($"Completion reward {grant.rewardId} kind does not match its payload.");
        if (string.IsNullOrWhiteSpace(grant.timedEntitlement.entitlementId)) throw new InvalidOperationException($"Completion reward {grant.rewardId} has no entitlement ID.");
        if (grant.timedEntitlement.durationSeconds <= 0) throw new InvalidOperationException($"Completion reward {grant.rewardId} duration must be greater than zero.");
    }
}
