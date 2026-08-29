namespace DefaultNamespace
{
    public static class RewardContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    public class RewardGrant
    {
        public string rewardId;
        public RewardKind kind;
        public string presentationKey;
        public InventoryItemRewardGrant inventoryItem;
        public CurrencyRewardGrant currency;
    }

    public class InventoryItemRewardGrant
    {
        public string itemCatalogId;
        public int quantity;
    }

    public class CurrencyRewardGrant
    {
        public string currencyId;
        public int amount;
    }

}
