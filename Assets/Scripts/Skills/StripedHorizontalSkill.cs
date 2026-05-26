using DefaultNamespace;
using Petals;

namespace Skills
{
    public class StripedHorizontalSkill : ISpecialSkill
    {
        public SpecialSkillType SkillType { get; set; } = SpecialSkillType.StripedHorizontal;
        public void OnMatchSuccess() { }
        public void OnMatchFail() { }
    }
}