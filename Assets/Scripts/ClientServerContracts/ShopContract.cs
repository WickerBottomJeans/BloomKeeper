using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// Current shop DTO version.
    /// </summary>
    public static class ShopContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    /// <summary>
    /// [Duong] Client's request to fetch data of a shop
    /// </summary>
    public class LoadShopRequest
    {
        public string shopId;
    }

    /// <summary>
    /// [Duong] Client's request to buy a shop offer
    /// </summary>
    public class BuyShopOfferRequest
    {
        public string shopId;
        public string offerId;
        public string buyShopOfferIdempotencyKey;
    }

    /// <summary>
    /// [Duong] Server's repsonse after client asked to buy something
    /// </summary>
    public class BuyShopOfferResponse
    {
        public int schemaVersion;
        public BuyShopOfferOutcome outcome;
        public BuyShopOfferRejectionReason? rejectionReason;
        public PlayerInventorySnapshot playerInventorySnapshot;
    }

    /// <summary>
    /// Shop data returned by LoadShop.
    /// </summary>
    public class LoadShopResponse
    {
        public int schemaVersion;
        public string shopId;
        public int offerCatalogRevision;
        public int shopfrontRevision;
        public List<ShopOfferViewData> offers = new List<ShopOfferViewData>();
    }

    /// <summary>
    /// Data for one shop offer.
    /// </summary>
    public class ShopOfferViewData
    {
        public string offerId;
        public string displayName;
        public ShopCostViewData cost;
        public List<ShopGrantViewData> grants = new List<ShopGrantViewData>();
    }

    /// <summary>
    /// Cost of one shop offer.
    /// </summary>
    public class ShopCostViewData
    {
        public string presentationKey;
        public int quantity;
    }

    /// <summary>
    /// One grant in a shop offer.
    /// </summary>
    public class ShopGrantViewData
    {
        public string grantId;
        public string presentationKey;
        public int? displayQuantity;
    }

    /// <summary>
    /// Result of a shop purchase request.
    /// </summary>
    public enum BuyShopOfferOutcome
    {
        Purchased = 1,
        Rejected = 2
    }

    /// <summary>
    /// Reason the server rejected a shop purchase.
    /// </summary>
    public enum BuyShopOfferRejectionReason
    {
        InsufficientCostItemQuantity = 1
    }
}
