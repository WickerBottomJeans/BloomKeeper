using System.Collections.Generic;

namespace DefaultNamespace
{
    public static class BoosterCatalogIds
    {
        public const string BloomWandFriendlyId = "booster_bloom_wand";
        public const string GardenersGloveFriendlyId = "booster_gardeners_glove";
    }

    public static class BoosterInventoryContract
    {
        public const int CurrentSchemaVersion = 1;
        public const string CanonicalStackId = "default";
    }

    public sealed class LoadBoosterInventoryResponse
    {
        public int schemaVersion = BoosterInventoryContract.CurrentSchemaVersion;
        public Dictionary<string, int> quantitiesByFriendlyId = new Dictionary<string, int>();
    }

    public sealed class ConsumeBoosterRequest
    {
        public string operationId;
        public string boosterFriendlyId;
    }

    public sealed class ConsumeBoosterResponse
    {
        public int schemaVersion = BoosterInventoryContract.CurrentSchemaVersion;
        public ConsumeBoosterOutcome outcome;
        public ConsumeBoosterRejectionReason? rejectionReason;
        public Dictionary<string, int> quantitiesByFriendlyId = new Dictionary<string, int>();
    }
}
