using System.Security.Cryptography;
using BloomKeeper.PlayFabFunctions.Models;

namespace BloomKeeper.PlayFabFunctions.Services;

public class RewardService
{
    private const int MaximumAwardChanceBasisPoints = 10000;

    /// <summary>
    /// [Duong] Rolls one result from a reward config.
    /// </summary>
    public RewardRollResult RollReward(RewardTableConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        int awardRoll = RandomNumberGenerator.GetInt32(MaximumAwardChanceBasisPoints);
        if (awardRoll >= config.awardChanceBasisPoints) return RewardRollResult.NoReward;

        int totalWeight = 0;
        foreach (WeightedRewardEntry entry in config.entries) totalWeight = checked(totalWeight + entry.weight);

        int selectedWeight = RandomNumberGenerator.GetInt32(totalWeight);
        int cumulativeWeight = 0;
        foreach (WeightedRewardEntry entry in config.entries)
        {
            cumulativeWeight += entry.weight;
            if (selectedWeight < cumulativeWeight) return RewardRollResult.CreateReward(entry.grant);
        }

        throw new InvalidOperationException("Reward roll did not resolve a weighted entry from the validated config.");
    }

    public IReadOnlyList<RewardRollResult> RollRewards(RewardTableConfig config, int rollCount)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (rollCount < 0) throw new ArgumentOutOfRangeException(nameof(rollCount), rollCount, "Reward roll count cannot be negative.");

        var results = new List<RewardRollResult>(rollCount);
        for (int rollIndex = 0; rollIndex < rollCount; rollIndex++) results.Add(RollReward(config));
        return results;
    }
}
