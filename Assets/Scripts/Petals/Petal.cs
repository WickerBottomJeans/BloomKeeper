using DefaultNamespace;
using System;

public class Petal : IBoardPiece
{
    public PetalType PetalType { get; }
    public SpecialSkillType Skill { get; }

    public Petal(PetalType petalType, SpecialSkillType skill = SpecialSkillType.None)
    {
        if ((skill == SpecialSkillType.PrismaticBloom) != (petalType == PetalType.None))
            throw new ArgumentException("Only Prismatic Bloom can have no petal type, and Prismatic Bloom cannot have a petal type.", nameof(petalType));

        PetalType = petalType;
        Skill = skill;
    }
    
    public Petal(Petal source) : this(source?.PetalType ?? throw new ArgumentNullException(nameof(source)), source.Skill) { }

    public bool IsMatchable()
    {
        return PetalType != PetalType.None;
    }
}
