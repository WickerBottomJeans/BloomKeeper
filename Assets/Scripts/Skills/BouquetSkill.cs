using DefaultNamespace;
using Petals;

namespace Skills
{
    public class BouquetSkill : ISpecialSkill
    {
        public SpecialSkillType SkillType { get; set; } = SpecialSkillType.Bouquet;
        public void OnMatchSuccess() { }
        public void OnMatchFail() { }
    }
}