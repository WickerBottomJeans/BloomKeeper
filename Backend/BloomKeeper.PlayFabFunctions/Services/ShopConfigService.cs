using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Services;

public class ShopConfigService
{
    private const string RemoteConfigBaseUrlEnvironmentVariable = "REMOTE_CONFIG_BASE_URL";
    private const int CurrentSchemaVersion = 1;
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly Uri remoteConfigBaseUri;

    public ShopConfigService()
    {
        string remoteConfigBaseUrl = Environment.GetEnvironmentVariable(RemoteConfigBaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(remoteConfigBaseUrl)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} is missing.");
        if (!Uri.TryCreate(remoteConfigBaseUrl, UriKind.Absolute, out Uri parsedRemoteConfigBaseUri)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} must be an absolute URL.");
        remoteConfigBaseUri = parsedRemoteConfigBaseUri.AbsoluteUri.EndsWith('/') ? parsedRemoteConfigBaseUri : new Uri($"{parsedRemoteConfigBaseUri.AbsoluteUri}/");
    }

    public async Task<ShopConfig> LoadShop(string shopId)
    {
        ValidateShopId(shopId);
        Task<ShopOfferCatalogConfig> offerCatalogTask = LoadConfig<ShopOfferCatalogConfig>($"shops/{shopId}/offers.json");
        Task<ShopfrontConfig> shopfrontTask = LoadConfig<ShopfrontConfig>($"shops/{shopId}/shopfront.json");
        await Task.WhenAll(offerCatalogTask, shopfrontTask);

        ShopOfferCatalogConfig offerCatalog = await offerCatalogTask;
        ShopfrontConfig shopfront = await shopfrontTask;
        ValidateShop(offerCatalog, shopfront);
        return new ShopConfig(shopId, offerCatalog, shopfront);
    }

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

    private static void ValidateShopId(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("Shop ID is missing.", nameof(shopId));
        if (!shopId.All(character => char.IsLower(character) || char.IsDigit(character) || character == '_')) throw new ArgumentException("Shop ID must contain only lowercase letters, digits, and underscores.", nameof(shopId));
    }

    private static void ValidateShop(ShopOfferCatalogConfig offerCatalog, ShopfrontConfig shopfront)
    {
        ValidateOfferCatalog(offerCatalog);
        ValidateShopfront(shopfront, offerCatalog.offers);
    }

    private static void ValidateOfferCatalog(ShopOfferCatalogConfig offerCatalog)
    {
        if (offerCatalog == null) throw new InvalidOperationException("Shop offer catalog contains invalid JSON.");
        if (offerCatalog.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Shop offer catalog schema version {offerCatalog.schemaVersion} is unsupported. Expected {CurrentSchemaVersion}.");
        if (offerCatalog.revision <= 0) throw new InvalidOperationException("Shop offer catalog revision must be greater than zero.");
        if (offerCatalog.offers == null || offerCatalog.offers.Count == 0) throw new InvalidOperationException("Shop offer catalog must contain at least one offer.");

        var observedOfferIds = new HashSet<string>(StringComparer.Ordinal);
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

    private static void ValidateOfferCost(ShopOfferConfig offer)
    {
        if (offer.cost == null) throw new InvalidOperationException($"Shop offer {offer.offerId} has no cost.");
        if (string.IsNullOrWhiteSpace(offer.cost.itemFriendlyId)) throw new InvalidOperationException($"Shop offer {offer.offerId} cost has no item Friendly ID.");
        if (string.IsNullOrWhiteSpace(offer.cost.presentationKey)) throw new InvalidOperationException($"Shop offer {offer.offerId} cost has no presentation key.");
        if (offer.cost.quantity <= 0) throw new InvalidOperationException($"Shop offer {offer.offerId} cost quantity must be greater than zero.");
    }

    private static void ValidateOfferGrants(ShopOfferConfig offer)
    {
        if (offer.grants == null || offer.grants.Count == 0) throw new InvalidOperationException($"Shop offer {offer.offerId} must contain at least one grant.");

        var observedGrantIds = new HashSet<string>(StringComparer.Ordinal);
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

    private static void ValidateInventoryItemGrant(string offerId, ShopGrantConfig grant)
    {
        if (grant.inventoryItem == null) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} kind does not match its payload.");
        if (string.IsNullOrWhiteSpace(grant.inventoryItem.itemFriendlyId)) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} has no inventory item Friendly ID.");
        if (grant.inventoryItem.quantity <= 0) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} inventory item quantity must be greater than zero.");
    }

    private static void ValidateUnlimitedLivesGrant(string offerId, ShopGrantConfig grant)
    {
        if (grant.unlimitedLives == null) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} kind does not match its payload.");
        if (grant.unlimitedLives.durationMinutes <= 0) throw new InvalidOperationException($"Shop offer {offerId} grant {grant.grantId} unlimited lives duration must be greater than zero.");
    }

    private static void ValidateShopfront(ShopfrontConfig shopfront, IReadOnlyList<ShopOfferConfig> offers)
    {
        if (shopfront == null) throw new InvalidOperationException("Shopfront contains invalid JSON.");
        if (shopfront.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Shopfront schema version {shopfront.schemaVersion} is unsupported. Expected {CurrentSchemaVersion}.");
        if (shopfront.revision <= 0) throw new InvalidOperationException("Shopfront revision must be greater than zero.");
        if (shopfront.offerIds == null || shopfront.offerIds.Count == 0) throw new InvalidOperationException("Shopfront must contain at least one offer ID.");

        var offersById = new HashSet<string>(offers.Select(offer => offer.offerId), StringComparer.Ordinal);
        var observedOfferIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string offerId in shopfront.offerIds)
        {
            if (string.IsNullOrWhiteSpace(offerId)) throw new InvalidOperationException("Shopfront contains an empty offer ID.");
            if (!observedOfferIds.Add(offerId)) throw new InvalidOperationException($"Shopfront contains duplicate offer ID {offerId}.");
            if (!offersById.Contains(offerId)) throw new InvalidOperationException($"Shopfront references unknown offer ID {offerId}.");
        }
    }
}
