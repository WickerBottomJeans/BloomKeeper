using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class MoveConstrainer : IConstrainer, IGameplayEventHandler<PlayerMoveCommittedEvent>
    {
        private readonly int initialMoveCount;
        private readonly int warningAtRemaining;
        private readonly bool hasValidWarningConfiguration;
        private int remainingMoveCount;
        private bool hasFailed;

        public MoveConstrainer(ConstrainerJson json)
        {
            initialMoveCount = json.moveLimit;
            remainingMoveCount = json.moveLimit;
            warningAtRemaining = json.warningAtRemaining;
            hasValidWarningConfiguration = warningAtRemaining > 0 && warningAtRemaining < initialMoveCount;
            if (!hasValidWarningConfiguration)
                Debug.LogWarning($"Move warning is disabled because warningAtRemaining must be greater than zero and lower than moveLimit. Received warningAtRemaining={warningAtRemaining}, moveLimit={initialMoveCount}.");
        }

        public event Action<ConstrainerFailureData> OnFailed;
        public event Action OnProgressUpdated;
        public ConstrainerType ConstrainerType { get; } = ConstrainerType.MoveLimit;
        public void Handle(PlayerMoveCommittedEvent gameplayEvent)
        {
            if (remainingMoveCount <= 0) return;

            remainingMoveCount--;
            OnProgressUpdated?.Invoke();
            if (remainingMoveCount <= 0 && !hasFailed)
            {
                hasFailed = true;
                OnFailed?.Invoke(new ConstrainerFailureData
                {
                    constrainerType = ConstrainerType,
                    failureText = "No moves left!"
                });
            }
        }

        public ConstrainerViewData GetViewData()
        {
            return new ConstrainerViewData
            {
                constrainerType = ConstrainerType.MoveLimit,
                constrainerText = remainingMoveCount.ToString(),
                isWarning = hasValidWarningConfiguration && remainingMoveCount > 0 && remainingMoveCount <= warningAtRemaining
            };
        }
    }
}
