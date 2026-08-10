using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class BoardResolutionInput
    {
        public IReadOnlyList<MatchGroup> MatchGroups { get; }
        public IReadOnlyList<SkillActivation> SkillActivations { get; }
        public IReadOnlyList<Vector2Int> PreferredSkillSpawnPositions { get; }
        public bool RequiresResolution => MatchGroups.Count > 0 || SkillActivations.Count > 0;

        public BoardResolutionInput(IReadOnlyList<MatchGroup> matchGroups, IReadOnlyList<SkillActivation> skillActivations, IReadOnlyList<Vector2Int> preferredSkillSpawnPositions)
        {
            if (matchGroups == null) throw new ArgumentNullException(nameof(matchGroups));
            if (skillActivations == null) throw new ArgumentNullException(nameof(skillActivations));
            if (preferredSkillSpawnPositions == null) throw new ArgumentNullException(nameof(preferredSkillSpawnPositions));
            if (matchGroups.Count > 0 && skillActivations.Count > 0) throw new ArgumentException("Board resolution input cannot open with matches and skill activations at the same time.");

            MatchGroups = new List<MatchGroup>(matchGroups).AsReadOnly();
            SkillActivations = new List<SkillActivation>(skillActivations).AsReadOnly();
            PreferredSkillSpawnPositions = new List<Vector2Int>(preferredSkillSpawnPositions).AsReadOnly();
        }
    }
}
