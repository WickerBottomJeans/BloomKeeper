using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

/// <summary>
/// Loaded offer and shopfront config for one shop.
/// </summary>
public class ShopConfig
{
    public string shopId;
    public ShopOfferCatalogConfig offerCatalog;
    public ShopfrontConfig shopfront;

    public ShopConfig(string shopId, ShopOfferCatalogConfig offerCatalog, ShopfrontConfig shopfront)
    {
        this.shopId = shopId ?? throw new ArgumentNullException(nameof(shopId));
        this.offerCatalog = offerCatalog ?? throw new ArgumentNullException(nameof(offerCatalog));
        this.shopfront = shopfront ?? throw new ArgumentNullException(nameof(shopfront));
    }
}

/// <summary>
/// All offers configured for one shop.
/// </summary>
public class ShopOfferCatalogConfig
{
    public int schemaVersion;
    public int revision;
    public List<ShopOfferConfig> offers = new List<ShopOfferConfig>();
}

/// <summary>
/// Config for one shop offer.
/// </summary>
public class ShopOfferConfig
{
    public string offerId;
    public string displayName;
    public bool enabled;
    public ShopCostConfig cost;
    public List<ShopGrantConfig> grants = new List<ShopGrantConfig>();
}

/// <summary>
/// Inventory item and quantity required to buy an offer.
/// </summary>
public class ShopCostConfig
{
    public CurrencyKind currencyKind;
    public string presentationKey;
    public int amount;
}

/// <summary>
/// Config for one grant in a shop offer.
/// </summary>
public class ShopGrantConfig
{
    public string grantId;
    public ShopGrantKind kind;
    public string presentationKey;
    public ShopInventoryItemGrantConfig inventoryItem;
    public ShopUnlimitedLivesGrantConfig unlimitedLives;
}

/// <summary>
/// Inventory item and quantity granted by an offer.
/// </summary>
public class ShopInventoryItemGrantConfig
{
    public string itemCatalogId;
    public int quantity;
}

/// <summary>
/// Duration granted by an unlimited-lives offer.
/// </summary>
public class ShopUnlimitedLivesGrantConfig
{
    public int durationSeconds;
}

/// <summary>
/// Offer IDs and order for one shopfront.
/// </summary>
public class ShopfrontConfig
{
    public int schemaVersion;
    public int revision;
    public List<string> offerIds = new List<string>();
}

/// <summary>
/// Types of grants supported by shop offers.
/// </summary>
public enum ShopGrantKind
{
    InventoryItem = 1,
    UnlimitedLives = 2
}
