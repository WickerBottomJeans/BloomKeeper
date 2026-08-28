namespace BloomKeeper.PlayFabFunctions.Models;

/// <summary>
/// [Duong] Stores the current shop purchase and its grant progress. Each player has exactly one of this at most
/// </summary>
public class ShopPurchaseData
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public string buyShopOfferIdempotencyKey;
    public ShopPurchaseStatus status;
    public ShopCostRecord shopCostRecord;
    public List<ShopGrantRecord> shopGrantRecords = new List<ShopGrantRecord>();
    
    public ShopPurchaseData()
    {
    }

    /// <summary>
    /// [Duong] Creates a current purchase with every grant pending.
    /// </summary>
    public ShopPurchaseData(string buyShopOfferIdempotencyKey, ShopCostConfig shopCostConfig, IReadOnlyList<ShopGrantConfig> shopGrantConfigs)
    {
        this.buyShopOfferIdempotencyKey = buyShopOfferIdempotencyKey;
        status = ShopPurchaseStatus.Pending;
        shopCostRecord = new ShopCostRecord { shopCostConfig = shopCostConfig, state = ShopCostState.Pending };
        foreach (ShopGrantConfig shopGrantConfig in shopGrantConfigs)
        {
            shopGrantRecords.Add(new ShopGrantRecord { shopGrantConfig = shopGrantConfig, state = ShopGrantState.Pending });
        }
    }

    /// <summary>
    /// [Duong] Records the applied cost, or returns false if already recorded.
    /// </summary>
    public bool RecordShopCostApplied(string expectedBuyShopOfferIdempotencyKey)
    {
        ValidateShopPurchaseIdentity(expectedBuyShopOfferIdempotencyKey);
        if (shopCostRecord.state == ShopCostState.Applied) return false;
        if (shopCostRecord.state != ShopCostState.Pending) throw new InvalidOperationException("Only a pending shop cost can be recorded as applied.");

        shopCostRecord.state = ShopCostState.Applied;
        return true;
    }

    /// <summary>
    /// [Duong] Records the reverted cost, or returns false if already recorded.
    /// </summary>
    public bool RecordShopCostReverted(string expectedBuyShopOfferIdempotencyKey)
    {
        ValidateShopPurchaseIdentity(expectedBuyShopOfferIdempotencyKey);
        if (shopCostRecord.state == ShopCostState.Reverted) return false;
        if (shopCostRecord.state != ShopCostState.Applied) throw new InvalidOperationException("Only an applied shop cost can be recorded as reverted.");

        shopCostRecord.state = ShopCostState.Reverted;
        return true;
    }

    /// <summary>
    /// [Duong] Records an applied grant, or returns false if already recorded.
    /// </summary>
    public bool RecordShopGrantApplied(string expectedBuyShopOfferIdempotencyKey, string grantId)
    {
        ValidateShopPurchaseIdentity(expectedBuyShopOfferIdempotencyKey);
        ShopGrantRecord shopGrantRecord = GetShopGrantRecord(grantId);
        if (shopGrantRecord.state == ShopGrantState.Applied) return false;
        if (shopGrantRecord.state != ShopGrantState.Pending) throw new InvalidOperationException($"Only a pending shop grant can be recorded as applied: '{grantId}'.");

        shopGrantRecord.state = ShopGrantState.Applied;
        return true;
    }

    /// <summary>
    /// [Duong] Records a reverted grant, or returns false if already recorded.
    /// </summary>
    public bool RecordShopGrantReverted(string expectedBuyShopOfferIdempotencyKey, string grantId)
    {
        ValidateShopPurchaseIdentity(expectedBuyShopOfferIdempotencyKey);
        ShopGrantRecord shopGrantRecord = GetShopGrantRecord(grantId);
        if (shopGrantRecord.state == ShopGrantState.Reverted) return false;
        if (shopGrantRecord.state != ShopGrantState.Applied) throw new InvalidOperationException($"Only an applied shop grant can be recorded as reverted: '{grantId}'.");

        shopGrantRecord.state = ShopGrantState.Reverted;
        return true;
    }

    /// <summary>
    /// [Duong] Records purchase completion, or returns false if already recorded.
    /// </summary>
    public bool RecordShopPurchaseCompleted(string expectedBuyShopOfferIdempotencyKey)
    {
        ValidateShopPurchaseIdentity(expectedBuyShopOfferIdempotencyKey);
        if (status == ShopPurchaseStatus.Completed) return false;
        if (status != ShopPurchaseStatus.Pending) throw new InvalidOperationException("Only a pending shop purchase can be recorded as completed.");

        status = ShopPurchaseStatus.Completed;
        return true;
    }

    /// <summary>
    /// [Duong] Throws when shop purchase data is invalid.
    /// </summary>
    public static void ValidateShopPurchaseData(ShopPurchaseData shopPurchaseData)
    {
        if (shopPurchaseData == null) throw new InvalidOperationException("Shop purchase data is missing.");
        if (shopPurchaseData.schemaVersion != ShopPurchaseData.CurrentSchemaVersion) throw new InvalidOperationException($"Shop purchase data has unsupported schema version: {shopPurchaseData.schemaVersion}.");
        if (!Guid.TryParseExact(shopPurchaseData.buyShopOfferIdempotencyKey, "N", out _)) throw new InvalidOperationException("Shop purchase data has an invalid buy shop offer idempotency key.");
        if (!Enum.IsDefined(typeof(ShopPurchaseStatus), shopPurchaseData.status)) throw new InvalidOperationException($"Shop purchase data contains an unsupported purchase status: {shopPurchaseData.status}.");
        if (shopPurchaseData.shopCostRecord == null) throw new InvalidOperationException("Shop purchase data has no cost record.");
        if (shopPurchaseData.shopCostRecord.shopCostConfig == null) throw new InvalidOperationException("Shop purchase data has no cost config snapshot.");
        if (!Enum.IsDefined(typeof(ShopCostState), shopPurchaseData.shopCostRecord.state)) throw new InvalidOperationException($"Shop purchase data contains an unsupported cost state: {shopPurchaseData.shopCostRecord.state}.");
        if (shopPurchaseData.shopGrantRecords == null || shopPurchaseData.shopGrantRecords.Count == 0) throw new InvalidOperationException("Shop purchase data has no grant records.");

        foreach (ShopGrantRecord shopGrantRecord in shopPurchaseData.shopGrantRecords)
        {
            if (shopGrantRecord == null) throw new InvalidOperationException("Shop purchase data contains a null grant record.");
            if (shopGrantRecord.shopGrantConfig == null) throw new InvalidOperationException("Shop purchase data contains a grant record without a config snapshot.");
            if (!Enum.IsDefined(typeof(ShopGrantState), shopGrantRecord.state)) throw new InvalidOperationException($"Shop purchase data contains an unsupported grant state: {shopGrantRecord.state}.");
        }
    }

    private void ValidateShopPurchaseIdentity(string expectedBuyShopOfferIdempotencyKey)
    {
        if (buyShopOfferIdempotencyKey != expectedBuyShopOfferIdempotencyKey) throw new InvalidOperationException("The stored shop purchase no longer matches the purchase being updated.");
    }

    private ShopGrantRecord GetShopGrantRecord(string grantId)
    {
        return shopGrantRecords.SingleOrDefault(shopGrantRecord => shopGrantRecord.shopGrantConfig.grantId == grantId) ?? throw new InvalidOperationException($"The shop purchase has no grant '{grantId}'.");
    }
}

/// <summary>
/// Says whether the current purchase still needs work.
/// </summary>
public enum ShopPurchaseStatus
{
    Pending = 1,
    Completed = 2
}

/// <summary>
/// [Duong] Stores the shop cost snapshot and its current state.
/// </summary>
public class ShopCostRecord
{
    public ShopCostConfig shopCostConfig;
    public ShopCostState state;
}

/// <summary>
/// [Duong] Current state of the recorded shop cost.
/// </summary>
public enum ShopCostState
{
    Pending = 1,
    Applied = 2,
    Reverted = 3
}

/// <summary>
/// [Duong] Stores one shop grant snapshot and its current state.
/// </summary>
public class ShopGrantRecord
{
    public ShopGrantConfig shopGrantConfig;
    public ShopGrantState state;
}

/// <summary>
/// [Duong] Current state of one recorded shop grant.
/// </summary>
public enum ShopGrantState
{
    Pending = 1,
    Applied = 2,
    Reverted = 3
}
