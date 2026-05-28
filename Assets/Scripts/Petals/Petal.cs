using System.Collections.Generic;
using DefaultNamespace;
using Petals;

public class Petal
{
    public PetalType PetalType { get; }
    public SpecialSkillType Skill { get; }

    public Petal(PetalType petalType, SpecialSkillType skill = SpecialSkillType.None)
    {
        PetalType = petalType;
        Skill = skill;
    }
}