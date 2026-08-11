using System;

namespace DefaultNamespace
{
    public sealed class BoosterViewData
    {
        public BoosterType BoosterType { get; }
        public int Amount { get; }

        public BoosterViewData(BoosterType boosterType, int amount)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Unsupported booster type.");
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Booster amount cannot be negative.");

            BoosterType = boosterType;
            Amount = amount;
        }
    }
}
