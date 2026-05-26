using DefaultNamespace;
using Petals;

namespace Skills
{
    public class ButterflySkill : ISpecialSkill
    {
        public SpecialSkillType SkillType { get; set; } = SpecialSkillType.Butterfly;
        public void OnMatchSuccess() { }
        public void OnMatchFail() { }
    }
}