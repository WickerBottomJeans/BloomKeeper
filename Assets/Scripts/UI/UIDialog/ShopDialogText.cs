using System;

namespace DefaultNamespace.UI
{
    public static class ShopDialogText
    {
        public const string ShopUnavailableTitle = "Shop unavailable";
        public const string ShopLoadFailureMessage = "Sorry, something went wrong.";
        public const string ShopLoadRetryMessage = ShopLoadFailureMessage;
        public const string PurchaseSuccessTitle = "Thank you";
        public const string PurchaseSuccessMessage = "Your purchase was successful!";
        public const string PurchaseFailureTitle = "Purchase failed";
        public const string PurchaseFailureMessage = "Sorry, something went wrong.";
        public const string PurchaseRetryMessage = PurchaseFailureMessage;

        public static string GetPurchaseRejectionMessage(BuyShopOfferRejectionReason rejectionReason)
        {
            return rejectionReason switch
            {
                BuyShopOfferRejectionReason.InsufficientCurrencyAmount => "Sorry, you don't have enough Diamonds.",
                BuyShopOfferRejectionReason.GrantApplicationFailed => "The purchase could not be completed. Your Diamonds were refunded.",
                BuyShopOfferRejectionReason.UnfinishedPurchase => "Sorry, you can't buy anything right now. Please contact customer support.",
                _ => throw new ArgumentOutOfRangeException(nameof(rejectionReason), rejectionReason, "Unsupported shop purchase rejection reason.")
            };
        }
    }
}
