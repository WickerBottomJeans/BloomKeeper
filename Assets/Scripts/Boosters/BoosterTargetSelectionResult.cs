using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boosters
{
    /// <summary>
    /// [Duong] Result after the player chooses booster targets, with an explicit outcome.
    /// </summary>
    public class BoosterTargetSelectionResult
    {
        private enum Outcome
        {
            Unavailable,
            Canceled,
            Selected
        }
        private readonly Outcome outcome;
        private readonly IReadOnlyList<Vector2Int> targets;
        
        public static BoosterTargetSelectionResult Unavailable { get; } = new BoosterTargetSelectionResult(Outcome.Unavailable, null);
        public static BoosterTargetSelectionResult Canceled { get; } = new BoosterTargetSelectionResult(Outcome.Canceled, null);

        public bool IsUnavailable => outcome == Outcome.Unavailable;
        public bool IsCanceled => outcome == Outcome.Canceled;
        public IReadOnlyList<Vector2Int> Targets => outcome == Outcome.Selected ? targets : throw new InvalidOperationException("Booster targeting without a selection has no targets.");
        
        public static BoosterTargetSelectionResult Selected(IReadOnlyList<Vector2Int> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            return new BoosterTargetSelectionResult(Outcome.Selected, new List<Vector2Int>(targets).AsReadOnly());
        }
        
        private BoosterTargetSelectionResult(Outcome outcome, IReadOnlyList<Vector2Int> targets)
        {
            this.outcome = outcome;
            this.targets = targets;
        }

    }
}
