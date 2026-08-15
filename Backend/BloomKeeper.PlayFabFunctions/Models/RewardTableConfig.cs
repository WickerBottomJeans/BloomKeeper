using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class RewardTableConfig
{
    public int schemaVersion = RewardContract.CurrentSchemaVersion;
    public string tableId;
    public string revision;
    public int awardChanceBasisPoints;
    public List<WeightedRewardEntry> entries = new List<WeightedRewardEntry>();
}

public class WeightedRewardEntry
{
    public int weight;
    public RewardGrant grant;
}
