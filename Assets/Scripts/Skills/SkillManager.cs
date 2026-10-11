using System;
using DefaultNamespace;
using DefaultNamespace.UI;

namespace Skills
{
    public static class SkillManager
    {
        public static SkillPlan UseSkill(SkillExecutionContext context, SkillActivation activation)
        {
            return activation.EffectType switch
            {
                SkillExecutionType.StripedHorizontal => StripedSkillExecutor.Execute(context, activation),
                SkillExecutionType.StripedVertical => StripedSkillExecutor.Execute(context, activation),
                SkillExecutionType.Bubble => BubbleSkillExecutor.Execute(context, activation),
                SkillExecutionType.PrismaticBloom => PrismaticBloomSkillExecutor.Execute(context, activation),
                SkillExecutionType.Butterfly => ButterflySkillExecutor.Execute(context, activation),
                SkillExecutionType.StripeStripeFusion => StripeStripeFusionExecutor.Execute(context, activation),
                _ => throw new ArgumentException("Skill not implemented.", nameof(activation.EffectType))
            };
        }
    }
}
