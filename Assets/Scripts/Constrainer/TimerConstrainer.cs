using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TimerConstrainer : IConstrainer, ITickableConstrainer
    {
        private readonly float initialSeconds;
        private readonly int warningAtRemaining;
        private readonly bool hasValidWarningConfiguration;
        private float remainingSeconds;
        private int lastDisplayedSeconds;
        private bool hasFailed;

        public TimerConstrainer(ConstrainerJson json)
        {
            initialSeconds = json.timeLimitSeconds;
            remainingSeconds = json.timeLimitSeconds;
            warningAtRemaining = json.warningAtRemaining;
            hasValidWarningConfiguration = warningAtRemaining > 0 && warningAtRemaining < initialSeconds;
            if (!hasValidWarningConfiguration)
                Debug.LogWarning($"Timer warning is disabled because warningAtRemaining must be greater than zero and lower than timeLimitSeconds. Received warningAtRemaining={warningAtRemaining}, timeLimitSeconds={initialSeconds}.");
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
                constrainerText = GetDisplayedSeconds().ToString(),
                isWarning = hasValidWarningConfiguration && GetDisplayedSeconds() > 0 && GetDisplayedSeconds() <= warningAtRemaining
            };
        }

        private int GetDisplayedSeconds() => Mathf.CeilToInt(remainingSeconds);
    }
}
