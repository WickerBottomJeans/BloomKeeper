using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Petals;

public static class PetalFactory
{
    private static readonly System.Random rng = new();
    public static PetalType[] RandomPetalTypes { get; } =
        ((PetalType[])System.Enum.GetValues(typeof(PetalType)))
        .Where(x => x != PetalType.None)
        .ToArray();

    /// <summary>
    /// Only use this to init a tilemap read from config
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Petal CreateForTileMap(TileData data)
    {
        PetalType petalType = data.petalType == PetalType.None
            ? GetRandomPetalType()
            : data.petalType;

        return new Petal(petalType, data.skillType);
    }
    
    private static PetalType GetRandomPetalType()
    {
        return RandomPetalTypes[rng.Next(RandomPetalTypes.Length)];
    }

    public static Petal CreateRandom()
    {
        return new Petal(GetRandomPetalType());
    }
    
    public static Petal CreatePetal(PetalType petalType, SpecialSkillType skillType = SpecialSkillType.None)
    {
        return new Petal(petalType, skillType);
    }
}