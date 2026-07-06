using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIJawCurtainTipProvider
    {
        private readonly UIJawCurtainTipConfig config;
        private readonly Dictionary<UIJawCurtainTipCategory, int> lastTipIndexByCategory = new();
        private readonly List<int> candidateIndices = new();

        public UIJawCurtainTipProvider(UIJawCurtainTipConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Get tip, already try to avoid same tip in a row.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public UIJawCurtainTip GetTip(UIJawCurtainTipCategory category = UIJawCurtainTipCategory.General)
        { 
            FillCandidates(category, true);
            if (candidateIndices.Count == 0)
                FillCandidates(category, false);
            if (candidateIndices.Count == 0)
                Debug.LogError($"No UI jaw curtain tip exists for category '{category}'.");

            int selectedIndex = candidateIndices[Random.Range(0, candidateIndices.Count)];
            lastTipIndexByCategory[category] = selectedIndex;
            return config.GetTip(selectedIndex);
        }

        private void FillCandidates(UIJawCurtainTipCategory category, bool skipLastTip)
        {
            candidateIndices.Clear();
            bool hasLastTip = lastTipIndexByCategory.TryGetValue(category, out int lastTipIndex);

            for (int i = 0; i < config.TipCount; i++)
            {
                if (!config.IsTipInCategory(i, category)) continue;
                if (skipLastTip && hasLastTip && i == lastTipIndex) continue;
                candidateIndices.Add(i);
            }
        }
    }
}
