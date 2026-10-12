using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

[CreateAssetMenu(fileName = "PetalSpriteCatalog", menuName = "BloomKeeper/Sprites/Petal Sprite Catalog")]
public sealed class PetalSpriteCatalog : ScriptableObject
{
    [SerializeField] private List<PetalSpriteEntry> entries = new();
    [SerializeField] private Sprite prismaticBloomSprite;

    public Sprite GetSprite(PetalType petalType, SpecialSkillType skillType)
    {
        if (skillType == SpecialSkillType.PrismaticBloom && petalType == PetalType.None && prismaticBloomSprite != null)
            return prismaticBloomSprite;

        foreach (PetalSpriteEntry entry in entries)
            if (entry.petalType == petalType && entry.skillType == skillType && entry.sprite != null)
                return entry.sprite;

        throw new InvalidOperationException($"No sprite for petal {petalType} with skill {skillType}.");
    }
}

[Serializable]
public sealed class PetalSpriteEntry
{
    public PetalType petalType;
    public SpecialSkillType skillType;
    public Sprite sprite;
}
