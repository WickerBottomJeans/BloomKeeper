using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class BoosterInventoryData
    {
        private readonly Dictionary<BoosterType, int> quantities;

        public IReadOnlyDictionary<BoosterType, int> Quantities => quantities;

        public BoosterInventoryData(IReadOnlyDictionary<BoosterType, int> quantities)
        {
            if (quantities == null) throw new ArgumentNullException(nameof(quantities));

            this.quantities = new Dictionary<BoosterType, int>(quantities);
            foreach (KeyValuePair<BoosterType, int> entry in this.quantities)
            {
                if (!Enum.IsDefined(typeof(BoosterType), entry.Key)) throw new ArgumentOutOfRangeException(nameof(quantities), entry.Key, "Booster inventory contains an unsupported booster type.");
                if (entry.Value < 0) throw new ArgumentOutOfRangeException(nameof(quantities), entry.Value, "Booster inventory quantities cannot be negative.");
            }
        }

        public int GetQuantity(BoosterType boosterType)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Unsupported booster type.");
            return quantities.TryGetValue(boosterType, out int quantity) ? quantity : 0;
        }
    }
}
