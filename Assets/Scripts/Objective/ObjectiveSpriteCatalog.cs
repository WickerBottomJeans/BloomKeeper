using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "ObjectiveSpriteCatalog", menuName = "BloomKeeper/Sprites/Objective Sprite Catalog")]
    public sealed class ObjectiveSpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<PetalObjectiveSpriteEntry> matchPetalSprites = new();
        [SerializeField] private Sprite spiderWebSprite;
        [SerializeField] private Sprite eggSprite;

        public Sprite GetSprite(ObjectiveType objectiveType, PetalType petalType)
        {
            if (objectiveType == ObjectiveType.ClearSpiderWeb && spiderWebSprite != null) return spiderWebSprite;
            if (objectiveType == ObjectiveType.CollectEgg && eggSprite != null) return eggSprite;
            if (objectiveType == ObjectiveType.Match)
            {
                foreach (PetalObjectiveSpriteEntry entry in matchPetalSprites)
                    if (entry.petalType == petalType && entry.sprite != null)
                        return entry.sprite;
            }

            throw new InvalidOperationException($"No sprite for objective {objectiveType} and petal {petalType}.");
        }
    }

    [Serializable]
    public sealed class PetalObjectiveSpriteEntry
    {
        public PetalType petalType;
        public Sprite sprite;
    }
}
