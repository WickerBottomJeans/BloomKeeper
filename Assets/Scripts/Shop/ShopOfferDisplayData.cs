using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// Prepared text and icon keys for one shop offer.
    /// </summary>
    public class ShopOfferDisplayData
    {
        public string OfferId { get; }
        public string DisplayName { get; }
        public string CostPresentationKey { get; }
        public string CostText { get; }
        public IReadOnlyList<ShopGrantDisplayData> Grants { get; }

        public ShopOfferDisplayData(string offerId, string displayName, string costPresentationKey, string costText, IReadOnlyList<ShopGrantDisplayData> shopGrantDisplayDataList)
        {
            OfferId = offerId;
            DisplayName = displayName;
            CostPresentationKey = costPresentationKey;
            CostText = costText;
            Grants = shopGrantDisplayDataList;
        }
    }
}
