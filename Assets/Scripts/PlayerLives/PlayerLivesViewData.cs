using System;

namespace DefaultNamespace
{
    public class PlayerLivesViewData
    {
        public int DisplayedLives { get; }
        public int MaximumLives { get; }
        public TimeSpan? RegenerationTimeRemaining { get; }

        public PlayerLivesViewData(int displayedLives, int maximumLives, TimeSpan? regenerationTimeRemaining)
        {
            DisplayedLives = displayedLives;
            MaximumLives = maximumLives;
            RegenerationTimeRemaining = regenerationTimeRemaining;
        }
    }
}
