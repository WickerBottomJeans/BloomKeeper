using System.Collections.Generic;

namespace DefaultNamespace
{
    public static class ShopContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    public class LoadShopRequest
    {
        public string shopId;
    }

    public class LoadShopResponse
    {
        public int schemaVersion;
        public string shopId;
        public int offerCatalogRevision;
        public int shopfrontRevision;
        public List<ShopOfferViewData> offers = new List<ShopOfferViewData>();
    }

    public class ShopOfferViewData
    {
        public string offerId;
        public string displayName;
        public ShopCostViewData cost;
        public List<ShopGrantViewData> grants = new List<ShopGrantViewData>();
    }

    public class ShopCostViewData
    {
        public string presentationKey;
        public int quantity;
    }

    public class ShopGrantViewData
    {
        public string grantId;
        public string presentationKey;
        public int? displayQuantity;
    }
}
