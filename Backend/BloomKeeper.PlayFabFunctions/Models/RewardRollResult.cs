using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class RewardRollResult
{
    public static RewardRollResult NoReward { get; } = new RewardRollResult(null);
    public bool HasReward => Reward != null;
    public RewardGrant? Reward { get; }

    private RewardRollResult(RewardGrant? reward)
    {
        Reward = reward;
    }

    public static RewardRollResult CreateReward(RewardGrant reward)
    {
        if (reward == null) throw new ArgumentNullException(nameof(reward));
        return new RewardRollResult(reward);
    }
}
