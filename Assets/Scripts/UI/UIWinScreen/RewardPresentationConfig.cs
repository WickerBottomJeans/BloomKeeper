using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "RewardPresentationConfig", menuName = "BloomKeeper/UI/Reward Presentation Config")]
    public class RewardPresentationConfig : ScriptableObject
    {
        [SerializeField] private List<RewardPresentationMapping> rewardPresentationMappings = new List<RewardPresentationMapping>();

        public Sprite GetRewardSprite(string presentationKey)
        {
            if (string.IsNullOrWhiteSpace(presentationKey)) throw new ArgumentException("Reward presentation key is missing.", nameof(presentationKey));

            RewardPresentationMapping matchingRewardPresentationMapping = null;
            foreach (RewardPresentationMapping rewardPresentationMapping in rewardPresentationMappings)
            {
                if (!string.Equals(rewardPresentationMapping.PresentationKey, presentationKey)) continue;
                if (matchingRewardPresentationMapping != null) throw new InvalidOperationException($"RewardPresentationConfig contains duplicate presentation key {presentationKey}.");
                matchingRewardPresentationMapping = rewardPresentationMapping;
            }

            if (matchingRewardPresentationMapping == null) throw new InvalidOperationException($"RewardPresentationConfig has no sprite for presentation key {presentationKey}.");
            return matchingRewardPresentationMapping.RewardSprite;
        }

        [Serializable]
        private class RewardPresentationMapping
        {
            [SerializeField] private string presentationKey;
            [SerializeField] private Sprite rewardSprite;

            public string PresentationKey => presentationKey;
            public Sprite RewardSprite => rewardSprite;
        }
    }
}
