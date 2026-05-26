using DefaultNamespace;
using Petals;

namespace Skills
{
    public class SunburstSkill : ISpecialSkill
    {
        public SpecialSkillType SkillType { get; set; } = SpecialSkillType.Sunburst;
        public void OnMatchSuccess() { }
        public void OnMatchFail() { }
    }
}