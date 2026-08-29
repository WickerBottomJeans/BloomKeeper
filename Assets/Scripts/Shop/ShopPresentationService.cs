using System;
using System.Collections.Generic;
using System.Globalization;

namespace DefaultNamespace
{
    /// <summary>
    /// Prepares shop offer text from validated server data.
    /// </summary>
    public class ShopPresentationService
    {
        /// <summary>
        /// Creates display data with formatted costs and grant values.
        /// </summary>
        public IReadOnlyList<ShopOfferDisplayData> CreateShopOfferDisplayData(LoadShopResponse loadShopResponse)
        {
            var shopOfferDisplayDataList = new List<ShopOfferDisplayData>(loadShopResponse.offers.Count);
            foreach (ShopOfferViewData shopOfferViewData in loadShopResponse.offers)
            {
                var shopGrantDisplayDataList = new List<ShopGrantDisplayData>(shopOfferViewData.grants.Count);
                foreach (ShopGrantViewData shopGrantViewData in shopOfferViewData.grants)
                    shopGrantDisplayDataList.Add(new ShopGrantDisplayData(shopGrantViewData.presentationKey, FormatShopGrantDisplayValue(shopGrantViewData)));

                shopOfferDisplayDataList.Add(new ShopOfferDisplayData(shopOfferViewData.offerId, shopOfferViewData.displayName, shopOfferViewData.cost.presentationKey, shopOfferViewData.cost.quantity.ToString(CultureInfo.InvariantCulture), shopGrantDisplayDataList.AsReadOnly()));
            }

            return shopOfferDisplayDataList.AsReadOnly();
        }

        private static string FormatShopGrantDisplayValue(ShopGrantViewData shopGrantViewData)
        {
            return shopGrantViewData.displayValueKind switch
            {
                ShopDisplayValueKind.Count => $"×{shopGrantViewData.displayValue.ToString(CultureInfo.InvariantCulture)}",
                ShopDisplayValueKind.DurationSeconds => FormatShopDuration(shopGrantViewData.displayValue),
                _ => throw new ArgumentOutOfRangeException(nameof(shopGrantViewData.displayValueKind), shopGrantViewData.displayValueKind, "Unsupported shop display value kind.")
            };
        }

        private static string FormatShopDuration(int durationSeconds)
        {
            int hours = durationSeconds / 3600;
            int minutes = durationSeconds % 3600 / 60;
            int seconds = durationSeconds % 60;
            var durationParts = new List<string>(3);
            if (hours > 0) durationParts.Add($"{hours.ToString(CultureInfo.InvariantCulture)}h");
            if (minutes > 0) durationParts.Add($"{minutes.ToString(CultureInfo.InvariantCulture)}m");
            if (seconds > 0) durationParts.Add($"{seconds.ToString(CultureInfo.InvariantCulture)}s");
            return string.Join(" ", durationParts);
        }
    }
}
