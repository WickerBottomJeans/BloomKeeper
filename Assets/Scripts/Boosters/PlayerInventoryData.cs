using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// Validated Diamond and booster quantities.
    /// </summary>
    public class PlayerInventoryData
    {
        private static readonly IReadOnlyDictionary<BoosterType, string> CatalogIdsByBoosterType = new Dictionary<BoosterType, string>
        {
            { BoosterType.BloomWand, PlayerInventoryCatalogIds.BloomWandCatalogId },
            { BoosterType.GardenersGlove, PlayerInventoryCatalogIds.GardenersGloveCatalogId }
        };
        private readonly Dictionary<BoosterType, int> boosterQuantities;

        public int DiamondQuantity { get; }
        public IReadOnlyDictionary<BoosterType, int> BoosterQuantities => boosterQuantities;

        /// <summary>
        /// Creates player inventory from the returned PlayFab stacks.
        /// </summary>
        public PlayerInventoryData(IReadOnlyDictionary<string, int> quantitiesByCatalogId)
        {
            if (quantitiesByCatalogId == null) throw new ArgumentNullException(nameof(quantitiesByCatalogId));

            foreach (KeyValuePair<string, int> entry in quantitiesByCatalogId)
            {
                if (string.IsNullOrWhiteSpace(entry.Key)) throw new ArgumentException("Player inventory catalog IDs cannot be empty.", nameof(quantitiesByCatalogId));
                if (entry.Value < 0) throw new ArgumentOutOfRangeException(nameof(quantitiesByCatalogId), entry.Value, $"Player inventory quantity for {entry.Key} cannot be negative.");
            }

            DiamondQuantity = GetInventoryItemQuantity(quantitiesByCatalogId, PlayerInventoryCatalogIds.DiamondsCatalogId);
            boosterQuantities = new Dictionary<BoosterType, int>();
            foreach (KeyValuePair<BoosterType, string> entry in CatalogIdsByBoosterType) boosterQuantities.Add(entry.Key, GetInventoryItemQuantity(quantitiesByCatalogId, entry.Value));
        }

        public int GetBoosterQuantity(BoosterType boosterType)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Unsupported booster type.");
            return boosterQuantities.TryGetValue(boosterType, out int quantity) ? quantity : 0;
        }

        public static string GetBoosterCatalogId(BoosterType boosterType)
        {
            if (!CatalogIdsByBoosterType.TryGetValue(boosterType, out string itemCatalogId)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster type is not supported by PlayFab inventory.");
            return itemCatalogId;
        }

        private static int GetInventoryItemQuantity(IReadOnlyDictionary<string, int> quantitiesByCatalogId, string itemCatalogId)
        {
            return quantitiesByCatalogId.TryGetValue(itemCatalogId, out int quantity) ? quantity : 0;
        }
    }
}
