using System.Collections.Generic;
using DefaultNamespace;
using Petals;

public static class PetalFactory
{
    private static readonly System.Random rng = new System.Random();

    /// <summary>
    /// Only use this to init a tilemap read from config
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Petal CreateForTileMap(TileData data)
    {
        PetalType petalType = data.petalType == PetalType.None
            ? (PetalType)rng.Next(1, System.Enum.GetValues(typeof(PetalType)).Length)
            : data.petalType;

        SpecialSkillType skill = data.skillType;

        return new Petal(petalType, skill);
    }
    
    public static Petal CreateRandom()
    {
        PetalType petalType = (PetalType)rng.Next(1, System.Enum.GetValues(typeof(PetalType)).Length);
        return new Petal(petalType);
    }
    
    public static Petal CreateSpecial(PetalType petalType, SpecialSkillType skillType)
    {
        return new Petal(petalType, skillType);
    }
}