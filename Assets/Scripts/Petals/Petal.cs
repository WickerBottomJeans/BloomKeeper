using System.Collections.Generic;
using DefaultNamespace;
using Petals;
using UnityEngine;

public class Petal
{
    public PetalType PetalType { get; }
    public SpecialSkillType Skill { get; }

    public Petal(PetalType petalType, SpecialSkillType skill = SpecialSkillType.None)
    {
        PetalType = petalType;
        Skill = skill;
    }
    
    public Petal(Petal source)
    {
        if (source == null)
        {
            Debug.LogError("Petal source is null");
            return;
        }

        PetalType = source.PetalType;
        Skill = source.Skill;
    }

    public bool IsMatchable()
    {
        return Skill != SpecialSkillType.PrismaticBloom;
    }
}
