namespace BloomKeeper.PlayFabFunctions.Models;

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

public class ShopOfferCatalogConfig
{
    public int schemaVersion;
    public int revision;
    public List<ShopOfferConfig> offers = new List<ShopOfferConfig>();
}

public class ShopOfferConfig
{
    public string offerId;
    public string displayName;
    public bool enabled;
    public ShopCostConfig cost;
    public List<ShopGrantConfig> grants = new List<ShopGrantConfig>();
}

public class ShopCostConfig
{
    public string itemFriendlyId;
    public string presentationKey;
    public int quantity;
}

public class ShopGrantConfig
{
    public string grantId;
    public ShopGrantKind kind;
    public string presentationKey;
    public ShopInventoryItemGrantConfig inventoryItem;
    public ShopUnlimitedLivesGrantConfig unlimitedLives;
}

public class ShopInventoryItemGrantConfig
{
    public string itemFriendlyId;
    public int quantity;
}

public class ShopUnlimitedLivesGrantConfig
{
    public int durationMinutes;
}

public class ShopfrontConfig
{
    public int schemaVersion;
    public int revision;
    public List<string> offerIds = new List<string>();
}

public enum ShopGrantKind
{
    InventoryItem = 1,
    UnlimitedLives = 2
}
