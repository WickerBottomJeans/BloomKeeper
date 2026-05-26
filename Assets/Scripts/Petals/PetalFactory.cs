using System.Collections.Generic;
using DefaultNamespace;
using Petals;

public static class PetalFactory
{
    private static readonly System.Random rng = new System.Random();

    public static Petal Create(TileData data)
    {
        PetalType petalType = data.petalType == PetalType.None
            ? (PetalType)rng.Next(1, System.Enum.GetValues(typeof(PetalType)).Length)
            : data.petalType;

        ISpecialSkill skill = data.skillType != SpecialSkillType.None
            ? CreateSkill(data.skillType)
            : null;

        return new Petal(petalType, skill);
    }

    private static ISpecialSkill CreateSkill(SpecialSkillType type)
    {
        switch (type)
        {
            default: throw new System.ArgumentException($"Unknown SpecialSkillType: {type}");
        }
    }
}