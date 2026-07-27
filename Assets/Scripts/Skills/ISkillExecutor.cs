using DefaultNamespace.UI;

namespace Skills
{
    public interface ISkillExecutor
    {
        SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation);
    }
}
