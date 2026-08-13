using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// Decides the level outcome from pending objective completion and constrainer failures when deciding is allowed.
    /// </summary>
    public class LevelOutcomeDecider
    {
        private readonly List<ConstrainerFailureData> pendingConstrainerFailures = new List<ConstrainerFailureData>();
        private bool canDecideOutcome;
        private bool isObjectiveCompletionPending;
        
        public event Action WinDecided;
        public event Action<IReadOnlyList<ConstrainerFailureData>> LossDecided;
        
        public bool IsDecided { get; private set; }
        
        public void HandleBoardIdleStateChanged(bool canDecide)
        {
            if (IsDecided) return;
            canDecideOutcome = canDecide;
            if (canDecideOutcome)
                TryDecide();
        }
        
        public void HandleAllObjectivesCompleted()
        {
            if (IsDecided) return;
            isObjectiveCompletionPending = true;
            TryDecide();
        }
        
        public void HandleConstrainerFailure(ConstrainerFailureData failureData)
        {
            if (failureData == null) throw new ArgumentNullException(nameof(failureData));
            if (IsDecided) return;

            pendingConstrainerFailures.Add(failureData);
            TryDecide();
        }
        
        private void TryDecide()
        {
            if (IsDecided || !canDecideOutcome) return;

            if (isObjectiveCompletionPending)
            {
                DecideWin();
                return;
            }

            if (pendingConstrainerFailures.Count == 0) return;

            IReadOnlyList<ConstrainerFailureData> failures = new List<ConstrainerFailureData>(pendingConstrainerFailures).AsReadOnly();
            pendingConstrainerFailures.Clear();
            DecideLoss(failures);
        }
        
        private void DecideWin()
        {
            IsDecided = true;
            WinDecided?.Invoke();
        }
        
        private void DecideLoss(IReadOnlyList<ConstrainerFailureData> failures)
        {
            IsDecided = true;
            LossDecided?.Invoke(failures);
        }
    }
}
