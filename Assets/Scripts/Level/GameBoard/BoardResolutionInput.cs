using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class BoardResolutionInput
    {
        public IReadOnlyList<MatchGroup> MatchGroups { get; }
        public IReadOnlyList<SkillActivation> SkillActivations { get; }
        public SwapContext? Swap { get; }
        public IReadOnlyList<Vector2Int> ClearPositions { get; }
        public bool RequiresResolution => MatchGroups.Count > 0 || SkillActivations.Count > 0 || ClearPositions.Count > 0;

        public BoardResolutionInput(IReadOnlyList<MatchGroup> matchGroups, IReadOnlyList<SkillActivation> skillActivations, SwapContext? swap = null, IReadOnlyList<Vector2Int> clearPositions = null)
        {
            if (matchGroups == null) throw new ArgumentNullException(nameof(matchGroups));
            if (skillActivations == null) throw new ArgumentNullException(nameof(skillActivations));
            if (matchGroups.Count > 0 && skillActivations.Count > 0) throw new ArgumentException("Board resolution input cannot open with matches and skill activations at the same time.");

            MatchGroups = new List<MatchGroup>(matchGroups).AsReadOnly();
            SkillActivations = new List<SkillActivation>(skillActivations).AsReadOnly();
            Swap = swap;
            ClearPositions = new List<Vector2Int>(clearPositions ?? Array.Empty<Vector2Int>()).AsReadOnly();
        }
    }
}
