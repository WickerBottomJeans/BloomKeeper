using DefaultNamespace;
using Petals;

namespace Skills
{
    public class StripedVerticalSkill : ISpecialSkill
    {
        public SpecialSkillType SkillType { get; set; } = SpecialSkillType.StripedVertical;
        public void OnMatchSuccess() { }
        public void OnMatchFail() { }
    }
}