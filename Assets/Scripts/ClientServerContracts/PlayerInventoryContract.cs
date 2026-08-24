using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// PlayFab catalog IDs for inventory items used by the game.
    /// </summary>
    public static class PlayerInventoryCatalogIds
    {
        public const string DiamondsCatalogId = "43dbe6b5-574b-43f7-88df-5ec3a5e564d0";
        public const string BloomWandCatalogId = "02a80453-0253-42db-9c87-024cb12263b6";
        public const string GardenersGloveCatalogId = "3f47e126-b690-4408-a452-416868ca4d91";
    }

    /// <summary>
    /// Current inventory DTO version and stack ID.
    /// </summary>
    public static class PlayerInventoryContract
    {
        public const int CurrentSchemaVersion = 1;
        public const string CanonicalStackId = "default";
    }

    /// <summary>
    /// PlayFab inventory stack quantities by catalog ID. Missing entries have no inventory stack.
    /// </summary>
    public class PlayerInventorySnapshot
    {
        public Dictionary<string, int> quantitiesByCatalogId = new Dictionary<string, int>();
    }

    /// <summary>
    /// Player inventory returned by LoadPlayerInventory.
    /// </summary>
    public class LoadPlayerInventoryResponse
    {
        public int schemaVersion = PlayerInventoryContract.CurrentSchemaVersion;
        public PlayerInventorySnapshot playerInventorySnapshot;
    }

    public class ConsumeBoosterRequest
    {
        public string boosterConsumptionIdempotencyKey;
        public string boosterCatalogId;
    }

    public class ConsumeBoosterResponse
    {
        public int schemaVersion = PlayerInventoryContract.CurrentSchemaVersion;
        public ConsumeBoosterOutcome outcome;
        public ConsumeBoosterRejectionReason? rejectionReason;
        public PlayerInventorySnapshot playerInventorySnapshot;
    }
}
