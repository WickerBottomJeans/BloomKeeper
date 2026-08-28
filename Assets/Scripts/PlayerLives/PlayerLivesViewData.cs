using System;

namespace DefaultNamespace
{
    public class PlayerLivesViewData
    {
        public PlayerLivesDisplayState DisplayState { get; }
        public int DisplayedLives { get; }
        public int MaximumLives { get; }
        public TimeSpan? RegenerationTimeRemaining { get; }
        public TimeSpan? UnlimitedLivesTimeRemaining { get; }

        public PlayerLivesViewData(PlayerLivesDisplayState displayState, int displayedLives, int maximumLives, TimeSpan? regenerationTimeRemaining, TimeSpan? unlimitedLivesTimeRemaining)
        {
            DisplayState = displayState;
            DisplayedLives = displayedLives;
            MaximumLives = maximumLives;
            RegenerationTimeRemaining = regenerationTimeRemaining;
            UnlimitedLivesTimeRemaining = unlimitedLivesTimeRemaining;
        }
    }
}
