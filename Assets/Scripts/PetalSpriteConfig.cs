using DefaultNamespace;
using UnityEngine;

[CreateAssetMenu(fileName = "PetalSpriteConfig", menuName = "Config/PetalSpriteConfig")]
public class PetalSpriteConfig : ScriptableObject
{
    [System.Serializable]
    public class PetalSpritePair
    {
        public PetalType petalType;
        public Sprite sprite;
        public SpecialSkillType skillType;
    }

    public PetalSpritePair[] entries;

    public Sprite GetSprite(PetalType type, SpecialSkillType skill)
    {
        foreach (var entry in entries)
        {
            if (entry.petalType == type && entry.skillType == skill)
            {
                return entry.sprite;
            }
        }

        Debug.LogWarning($"No sprite found for PetalType: {type} and SpecialSkillType: {skill}");
        return null;
    }
}