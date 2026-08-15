using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class PendingRewardsData
{
    public const int CurrentSchemaVersion = 1;
    public int schemaVersion = CurrentSchemaVersion;
    public List<PendingRewardBatch> batches = new List<PendingRewardBatch>();
}

public class PendingRewardBatch
{
    public string rewardBatchId;
    public List<RewardGrant?> rewardRolls = new List<RewardGrant?>();
}
