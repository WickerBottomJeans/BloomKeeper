using System;
using System.Collections.Generic;
using DefaultNamespace;

namespace Boosters
{
    public sealed class BoosterUseCoordinator
    {
        public event Action<BoosterType> BoosterUseApproved;

        
        /// <summary>
        /// Just place holder for now. MUST change later
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<BoosterType> GetAvailableBoosters() => new[] { BoosterType.BloomWand, BoosterType.GardenersGlove };

        public void RequestUse(BoosterType boosterType)
        {
            BoosterUseApproved?.Invoke(boosterType);
        }
    }
}
