using System;
using DefaultNamespace;

namespace Boosters
{
    public static class BoosterManager
    {
        public static IBoosterChooser CreateChooser(BoosterType boosterType)
        {
            return boosterType switch
            {
                BoosterType.BloomWand => new BloomWandChooser(),
                BoosterType.GardenersGlove => new GardenersGloveChooser(),
                _ => throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not supported.")
            };
        }
    }
}
