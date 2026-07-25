using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.Utility
{
    public static class GameTimeService
    {
        private static readonly HashSet<object> pauseOwners = new();
        private static float timeScaleBeforePause;

        public static event Action<bool> PauseStateChanged;

        public static bool IsPaused => pauseOwners.Count > 0;

        public static void RequestPause(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!pauseOwners.Add(owner))
                throw new InvalidOperationException("The same owner cannot request a game-time pause more than once.");
            if (pauseOwners.Count > 1)
                return;

            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            PauseStateChanged?.Invoke(true);
        }

        public static void ReleasePause(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!pauseOwners.Remove(owner))
                throw new InvalidOperationException("An owner cannot release a game-time pause it did not request.");
            if (pauseOwners.Count > 0)
                return;

            Time.timeScale = timeScaleBeforePause;
            PauseStateChanged?.Invoke(false);
        }
    }
}
