using System.Collections.Generic;

namespace DefaultNamespace.UI
{
    public class TurnResolution
    {
        public MatchResolveResult InitialMatch { get; }
        public IReadOnlyList<SkillResolutionWave> SkillWaves { get; }

        public TurnResolution(MatchResolveResult initialMatch, IReadOnlyList<SkillResolutionWave> skillWaves)
        {
            InitialMatch = initialMatch;
            SkillWaves = skillWaves;
        }
    }

    public class SkillResolutionWave
    {
        public MatchResolveResult Resolution { get; }
        public IReadOnlyList<SkillUseResult> SkillResults { get; }

        public SkillResolutionWave(MatchResolveResult resolution, IReadOnlyList<SkillUseResult> skillResults)
        {
            Resolution = resolution;
            SkillResults = skillResults;
        }
    }
}
