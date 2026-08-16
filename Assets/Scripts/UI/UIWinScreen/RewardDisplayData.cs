using System;
using System.Collections.Generic;

namespace DefaultNamespace.UI
{
    public class RewardDisplayData
    {
        public IReadOnlyList<string> CompletionRewardPresentationKeys { get; }
        public int Amount { get; }

        public RewardDisplayData(IReadOnlyList<string> completionRewardPresentationKeys, int amount)
        {
            CompletionRewardPresentationKeys = completionRewardPresentationKeys ?? throw new ArgumentNullException(nameof(completionRewardPresentationKeys));
            if (completionRewardPresentationKeys.Count > amount) throw new ArgumentException("Completion rewards exceed the display amount.", nameof(completionRewardPresentationKeys));

            Amount = amount;
        }
    }
}
