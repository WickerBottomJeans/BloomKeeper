using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public abstract class RewardFulfillmentResult
{
    public RewardGrant Reward { get; }

    protected RewardFulfillmentResult(RewardGrant reward)
    {
        Reward = reward ?? throw new ArgumentNullException(nameof(reward));
    }
}

public class InventoryItemRewardFulfillmentResult : RewardFulfillmentResult
{
    public InventoryItemRewardFulfillmentResult(RewardGrant reward) : base(reward)
    {
    }
}
