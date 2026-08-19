using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "ShopSpriteCatalog", menuName = "BloomKeeper/Shop/Shop Sprite Catalog")]
    public class ShopSpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<ShopSpriteEntry> entries = new List<ShopSpriteEntry>();

        private Dictionary<string, Sprite> spritesByPresentationKey;

        public Sprite GetSprite(string presentationKey)
        {
            if (string.IsNullOrWhiteSpace(presentationKey)) throw new ArgumentException("Presentation key is missing.", nameof(presentationKey));
            BuildSpriteLookup();
            if (!spritesByPresentationKey.TryGetValue(presentationKey, out Sprite sprite)) throw new KeyNotFoundException($"Shop sprite catalog has no sprite for presentation key {presentationKey}.");

            return sprite;
        }

        private void OnEnable()
        {
            spritesByPresentationKey = null;
        }

        private void BuildSpriteLookup()
        {
            if (spritesByPresentationKey != null) return;

            spritesByPresentationKey = new Dictionary<string, Sprite>(entries.Count, StringComparer.Ordinal);
            foreach (ShopSpriteEntry entry in entries)
            {
                if (entry == null) throw new InvalidOperationException("Shop sprite catalog contains a null entry.");
                if (string.IsNullOrWhiteSpace(entry.presentationKey)) throw new InvalidOperationException("Shop sprite catalog contains an entry without a presentation key.");
                if (entry.sprite == null) throw new InvalidOperationException($"Shop sprite catalog entry {entry.presentationKey} has no sprite.");
                if (!spritesByPresentationKey.TryAdd(entry.presentationKey, entry.sprite)) throw new InvalidOperationException($"Shop sprite catalog contains duplicate presentation key {entry.presentationKey}.");
            }
        }
    }

    [Serializable]
    public class ShopSpriteEntry
    {
        public string presentationKey;
        public Sprite sprite;
    }
}
