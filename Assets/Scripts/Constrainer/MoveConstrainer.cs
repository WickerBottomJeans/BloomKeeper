using System;

namespace DefaultNamespace
{
    public class MoveConstrainer : IConstrainer, IGameplayEventHandler
    {
        private readonly int initialMoveCount;
        private int remainingMoveCount;
        private bool hasFailed;

        public MoveConstrainer(ConstrainerJson json)
        {
            initialMoveCount = json.moveLimit;
            remainingMoveCount = json.moveLimit;
        }

        public event Action<ConstrainerFailureData> OnFailed;
        public event Action OnProgressUpdated;
        public ConstrainerType ConstrainerType { get; } = ConstrainerType.MoveLimit;
        public Type HandledEventType => typeof(PlayerMoveCommittedEvent);

        public void Handle(IGameplayEvent e)
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
                constrainerText = remainingMoveCount.ToString()
            };
        }
    }
}
