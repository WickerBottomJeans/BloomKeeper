using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TimerConstrainer : IConstrainer, ITickableConstrainer
    {
        private readonly float initialSeconds;
        private float remainingSeconds;
        private int lastDisplayedSeconds;
        private bool hasFailed;

        public TimerConstrainer(ConstrainerJson json)
        {
            initialSeconds = json.timeLimitSeconds;
            remainingSeconds = json.timeLimitSeconds;
            lastDisplayedSeconds = GetDisplayedSeconds();
        }

        public event Action<ConstrainerFailureData> OnFailed;
        public event Action OnProgressUpdated;
        public ConstrainerType ConstrainerType { get; } = ConstrainerType.TimeLimit;

        public void Tick(float deltaTime)
        {
            if (remainingSeconds <= 0) return;

            remainingSeconds = Mathf.Max(0, remainingSeconds - deltaTime);
            int displayedSeconds = GetDisplayedSeconds();
            if (displayedSeconds == lastDisplayedSeconds) return;

            lastDisplayedSeconds = displayedSeconds;
            OnProgressUpdated?.Invoke();

            if (remainingSeconds <= 0 && !hasFailed)
            {
                hasFailed = true;
                OnFailed?.Invoke(new ConstrainerFailureData
                {
                    constrainerType = ConstrainerType,
                    failureText = "Time's up!"
                });
            }
        }

        public ConstrainerViewData GetViewData()
        {
            return new ConstrainerViewData
            {
                constrainerType = ConstrainerType.TimeLimit,
                constrainerText = GetDisplayedSeconds().ToString()
            };
        }

        private int GetDisplayedSeconds() => Mathf.CeilToInt(remainingSeconds);
    }
}
