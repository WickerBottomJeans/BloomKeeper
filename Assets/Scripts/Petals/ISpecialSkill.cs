using DefaultNamespace;

namespace Petals
{
    public interface ISpecialSkill
    {
        void OnMatchSuccess();
        void OnMatchFail();
        SpecialSkillType SkillType { get; set; }
    }
}