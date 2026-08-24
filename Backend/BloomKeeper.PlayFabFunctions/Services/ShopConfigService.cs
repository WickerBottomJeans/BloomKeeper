using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// Loads and validates shop config from R2.
/// </summary>
public class ShopConfigService
{
    private const string RemoteConfigBaseUrlEnvironmentVariable = "REMOTE_CONFIG_BASE_URL";
    private const int CurrentSchemaVersion = 1;
    private const int MaximumInventoryOperationsPerBatch = 50;
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly Uri remoteConfigBaseUri;

    /// <summary>
    /// Reads the R2 config base URL.
    /// </summary>
    public ShopConfigService()
    {
        string remoteConfigBaseUrl = Environment.GetEnvironmentVariable(RemoteConfigBaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(remoteConfigBaseUrl)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} is missing.");
        if (!Uri.TryCreate(remoteConfigBaseUrl, UriKind.Absolute, out Uri parsedRemoteConfigBaseUri)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} must be an absolute URL.");
        remoteConfigBaseUri = parsedRemoteConfigBaseUri.AbsoluteUri.EndsWith('/') ? parsedRemoteConfigBaseUri : new Uri($"{parsedRemoteConfigBaseUri.AbsoluteUri}/");
    }

    /// <summary>
    /// Loads and validates one shop's offers and shopfront.
    /// </summary>
    public async Task<ShopConfig> LoadShop(string shopId)
    {
        ValidateShopId(shopId);

        // Load offers and shopfront together.
        Task<ShopOfferCatalogConfig> offerCatalogTask = LoadConfig<ShopOfferCatalogConfig>($"shops/{shopId}/offers.json");
        Task<ShopfrontConfig> shopfrontTask = LoadConfig<ShopfrontConfig>($"shops/{shopId}/shopfront.json");
        await Task.WhenAll(offerCatalogTask, shopfrontTask);

        ShopOfferCatalogConfig offerCatalog = await offerCatalogTask;
        ShopfrontConfig shopfront = await shopfrontTask;
        // Validate both files as one shop.
        ValidateShop(offerCatalog, shopfront);
        return new ShopConfig(shopId, offerCatalog, shopfront);
    }

    /// <summary>
    /// Loads an offer that is currently displayed and enabled.
    /// </summary>
    public async Task<ShopOfferConfig> LoadPurchasableOffer(string shopId, string offerId)
    {
        ValidateOfferId(offerId);
        ShopConfig shopConfig = await LoadShop(shopId);

        if (!shopConfig.shopfront.offerIds.Contains(offerId)) throw new InvalidOperationException($"Shop {shopId} does not display offer {offerId}.");

        ShopOfferConfig shopOffer = shopConfig.offerCatalog.offers.Single(offer => offer.offerId == offerId);
        if (!shopOffer.enabled) throw new InvalidOperationException($"Shop offer {offerId} is disabled.");

        return shopOffer;
    }

    /// <summary>
    /// Loads one JSON config file from R2.
    /// </summary>
    private async Task<T> LoadConfig<T>(string relativePath)
    {
        Uri configUri = new Uri(remoteConfigBaseUri, relativePath);
        using HttpResponseMessage response = await HttpClient.GetAsync(configUri);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Failed to load shop config from {configUri}. HTTP status: {(int)response.StatusCode} {response.StatusCode}.");

        string json = await response.Content.ReadAsStringAsync();
        try
        {
            T config = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            return config ?? throw new InvalidOperationException($"Shop config at {configUri} contains invalid JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Shop config at {configUri} contains invalid JSON.", exception);
        }
    }

    /// <summary>
    /// Checks that a shop ID is valid for its R2 path.
    /// </summary>
    private static void ValidateShopId(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("Shop ID is missing.", nameof(shopId));
        if (!shopId.All(character => char.IsLower(character) || char.IsDigit(character) || character == '_')) throw new ArgumentException("Shop ID must contain only lowercase letters, digits, and underscores.", nameof(shopId));
    }

    /// <summary>
    /// Checks that an offer ID was provided.
    /// </summary>
    private static void ValidateOfferId(string offerId)
    {
        if (string.IsNullOrWhiteSpace(offerId)) throw new ArgumentException("Offer ID is missing.", nameof(offerId));
    }

    /// <summary>
    /// Validates a shop's offers and shopfront.
    /// </summary>
    private static void ValidateShop(ShopOfferCatalogConfig offerCatalog, ShopfrontConfig shopfront)
    {
        ValidateOfferCatalog(offerCatalog);
        ValidateShopfront(shopfront, offerCatalog.offers);
    }

    /// <summary>
    /// Validates all offers in a shop config.
    /// </summary>
    private static void ValidateOfferCatalog(ShopOfferCatalogConfig offerCatalog)
    {
        if (offerCatalog == null) throw new InvalidOperationException("Shop offer catalog contains invalid JSON.");
        if (offerCatalog.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Shop offer catalog schema version {offerCatalog.schemaVersion} is unsupported. Expected {CurrentSchemaVersion}.");
        if (offerCatalog.revision <= 0) throw new InvalidOperationException("Shop offer catalog revision must be greater than zero.");
        if (offerCatalog.offers == null || offerCatalog.offers.Count == 0) throw new InvalidOperationException("Shop offer catalog must contain at least one offer.");

        var observedOfferIds = new HashSet<string>();
        foreach (ShopOfferConfig offer in offerCatalog.offers)
        {
            if (offer == null) throw new InvalidOperationException("Shop offer catalog contains a null offer.");
            if (string.IsNullOrWhiteSpace(offer.offerId)) throw new InvalidOperationException("Shop offer catalog contains an offer without an offer ID.");
            if (!observedOfferIds.Add(offer.offerId)) throw new InvalidOperationException($"Shop offer catalog contains duplicate offer ID {offer.offerId}.");
            if (string.IsNullOrWhiteSpace(offer.displayName)) throw new InvalidOperationException($"Shop offer {offer.offerId} has no display name.");
            ValidateOfferCost(offer);
            ValidateOfferGrants(offer);
        }
    }

    /// <summary>
    /// Validates an offer's inventory cost.
    /// </summary>
    private static void ValidateOfferCost(ShopOfferConfig offer)
    {
        if (offer.cost == null) throw new InvalidOperationException($"Shop offer {offer.offerId} has no cost.");
        if (string.IsNullOrWhiteSpace(offer.cost.itemCatalogId)) throw new InvalidOperationException($"Shop offer {offer.offerId} cost has no item catalog ID.");
        if (string.IsNullOrWhiteSpace(offer.cost.presentationKey)) throw new InvalidOperationException($"Shop offer {offer.offerId} cost has no presentation key.");
        if (offer.cost.quantity <= 0) throw new InvalidOperationException($"Shop offer {offer.offerId} cost quantity must be greater than zero.");
    }

    /// <summary>
    /// Validates every grant in an offer.
    /// </summary>
    private static void ValidateOfferGrants(ShopOfferConfig offer)
    {
        if (offer.grants == null || offer.grants.Count == 0) throw new InvalidOperationException($"Shop offer {offer.offerId} must contain at least one grant.");
        int inventoryItemGrantCount = offer.grants.Count(grant => grant?.kind == ShopGrantKind.InventoryItem);
        if (inventoryItemGrantCount + 1 > MaximumInventoryOperationsPerBatch) throw new InvalidOperationException($"Shop offer {offer.offerId} exceeds the {MaximumInventoryOperationsPerBatch}-operation inventory batch limit.");

        var observedGrantIds = new HashSet<string>();
        foreach (ShopGrantConfig grant in offer.grants)
        {
            if (grant == null) throw new InvalidOperationException($"Shop offer {offer.offerId} contains a null grant.");
            if (string.IsNullOrWhiteSpace(grant.grantId)) throw new InvalidOperationException($"Shop offer {offer.offerId} contains a grant without a grant ID.");
            if (!observedGrantIds.Add(grant.grantId)) throw new InvalidOperationException($"Shop offer {offer.offerId} contains duplicate grant ID {grant.grantId}.");
            if (!Enum.IsDefined(typeof(ShopGrantKind), grant.kind)) throw new InvalidOperationException($"Shop offer {offer.offerId} grant {grant.grantId} has an unsupported kind {grant.kind}.");
            if (string.IsNullOrWhiteSpace(grant.presentationKey)) throw new InvalidOperationException($"Shop offer {offer.offerId} grant {grant.grantId} has no presentation key.");

            int payloadCount = (grant.inventoryItem != null ? 1 : 0) + (grant.unlimitedLives != null ? 1 : 0);
            if (payloadCount != 1) throw new InvalidOperationException($"Shop offer {offer.offerId} grant {grant.grantId} must contain exactly one grant payload.");

            switch (grant.kind)
            {
                case ShopGrantKind.InventoryItem:
                    ValidateInventoryItemGrant(offer.offerId, grant);
                    break;
                case ShopGrantKind.UnlimitedLives:
                    ValidateUnlimitedLivesGrant(offer.offerId, grant);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grant.kind), grant.kind, "Unsupported shop grant kind.");
            }
        }
    }

    /// <summary>
    /// Validates an inventory-item grant.
    /// </summary>
    private static void ValidateInventoryItemGrant(string offerId, ShopGrantConfig grant)
    {
        if (grant.inventoryItem == null) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} kind does not match its payload.");
        if (string.IsNullOrWhiteSpace(grant.inventoryItem.itemCatalogId)) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} has no inventory item catalog ID.");
        if (grant.inventoryItem.quantity <= 0) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} inventory item quantity must be greater than zero.");
    }

    /// <summary>
    /// Validates an unlimited-lives grant.
    /// </summary>
    private static void ValidateUnlimitedLivesGrant(string offerId, ShopGrantConfig grant)
    {
        if (grant.unlimitedLives == null) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} kind does not match its payload.");
        if (grant.unlimitedLives.durationMinutes <= 0) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} unlimited lives duration must be greater than zero.");
    }

    /// <summary>
    /// Validates the offers and order in a shopfront.
    /// </summary>
    private static void ValidateShopfront(ShopfrontConfig shopfront, IReadOnlyList<ShopOfferConfig> offers)
    {
        if (shopfront == null) throw new InvalidOperationException("Shopfront contains invalid JSON.");
        if (shopfront.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Shopfront schema version {shopfront.schemaVersion} is unsupported. Expected {CurrentSchemaVersion}.");
        if (shopfront.revision <= 0) throw new InvalidOperationException("Shopfront revision must be greater than zero.");
        if (shopfront.offerIds == null || shopfront.offerIds.Count == 0) throw new InvalidOperationException("Shopfront must contain at least one offer ID.");

        var offersById = new HashSet<string>(offers.Select(offer => offer.offerId));
        var observedOfferIds = new HashSet<string>();
        foreach (string offerId in shopfront.offerIds)
        {
            if (string.IsNullOrWhiteSpace(offerId)) throw new InvalidOperationException("Shopfront contains an empty offer ID.");
            if (!observedOfferIds.Add(offerId)) throw new InvalidOperationException($"Shopfront contains duplicate offer ID {offerId}.");
            if (!offersById.Contains(offerId)) throw new InvalidOperationException($"Shopfront references unknown offer ID {offerId}.");
        }
    }
}
