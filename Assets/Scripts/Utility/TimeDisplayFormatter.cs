using System;

namespace DefaultNamespace.Utility
{
    /// <summary>
    /// Formats remaining time for compact countdown displays.
    /// </summary>
    public static class TimeDisplayFormatter
    {
        /// <summary>
        /// Formats a countdown as mm:ss below one hour, or hh:mm from one hour onward.
        /// </summary>
        public static string FormatCountdown(TimeSpan timeRemaining)
        {
            if (timeRemaining < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeRemaining), timeRemaining, "Countdown time cannot be negative.");

            long totalRemainingSeconds = (long)Math.Ceiling(timeRemaining.TotalSeconds);
            if (totalRemainingSeconds < 3600) return $"{totalRemainingSeconds / 60:00}:{totalRemainingSeconds % 60:00}";

            long totalRemainingMinutes = (long)Math.Ceiling(timeRemaining.TotalMinutes);
            return $"{totalRemainingMinutes / 60:00}:{totalRemainingMinutes % 60:00}";
        }
    }
}
