using System.Collections.Generic;
using DefaultNamespace;
using Petals;

public class Petal
{
    public PetalType PetalType { get; }
    public ISpecialSkill Skill { get; }

    public Petal(PetalType petalType, ISpecialSkill skill = null)
    {
        PetalType = petalType;
        Skill = skill;
    }

    public void TriggerMatchSuccess() => Skill?.OnMatchSuccess();
    public void TriggerMatchFail()    => Skill?.OnMatchFail();
}