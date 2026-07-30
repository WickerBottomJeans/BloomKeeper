using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;

namespace Skills
{
    public static class SkillManager
    {
        private static readonly IReadOnlyDictionary<SpecialSkillType, ISkillExecutor> Executors = new Dictionary<SpecialSkillType, ISkillExecutor>
        {
            { SpecialSkillType.StripedHorizontal, new StripedSkillExecutor() },
            { SpecialSkillType.StripedVertical, new StripedSkillExecutor() },
            { SpecialSkillType.Bubble, new BubbleSkillExecutor() },
            { SpecialSkillType.PrismaticBloom, new PrismaticBloomSkillExecutor() },
            { SpecialSkillType.Butterfly, new ButterflySkillExecutor() }
        };

        public static List<SkillUseResult> UseSkills(Tile[,] grid, IReadOnlyList<SkillActivation> activations, IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups)
        {
            var results = new List<SkillUseResult>(activations.Count);
            var context = new SkillExecutionContext(grid, objectiveTargetGroups);

            foreach (SkillActivation activation in activations)
            {
                if (!Executors.TryGetValue(activation.EffectType, out ISkillExecutor executor))
                    throw new ArgumentException("Skill not implemented.", nameof(activation.EffectType));

                results.Add(executor.Execute(context, activation));
            }
            return results;
        }
    }
}
