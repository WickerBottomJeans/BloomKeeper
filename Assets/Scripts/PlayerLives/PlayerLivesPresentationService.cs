using System;

namespace DefaultNamespace
{
    public class PlayerLivesPresentationService
    {
        private int availableLives;
        private int maximumLives;
        private int regenerationIntervalSeconds;
        private DateTimeOffset? regenerationAnchorUtc;
        private bool hasServerLivesSnapshot;

        public event Action ServerLivesSnapshotChanged;

        /// <summary>
        /// [Duong] Update this service with fresh data from the server
        /// </summary>
        public void ReplaceServerLivesSnapshot(PlayerLivesSnapshot serverLivesSnapshot)
        {
            PlayerLivesContract.ValidateSnapshot(serverLivesSnapshot);
            availableLives = serverLivesSnapshot.availableLives;
            maximumLives = serverLivesSnapshot.maximumLives;
            regenerationIntervalSeconds = serverLivesSnapshot.regenerationIntervalSeconds;
            regenerationAnchorUtc = serverLivesSnapshot.regenerationAnchorUtc;
            hasServerLivesSnapshot = true;
            ServerLivesSnapshotChanged?.Invoke();
        }

        public PlayerLivesViewData CreateCurrentLivesViewData(DateTimeOffset nowUtc)
        {
            if (!hasServerLivesSnapshot) throw new InvalidOperationException("Cannot create lives view data before receiving a server lives snapshot.");
            if (!regenerationAnchorUtc.HasValue) return new PlayerLivesViewData(availableLives, maximumLives, null);

            DateTimeOffset anchorUtc = regenerationAnchorUtc.Value;
            DateTimeOffset projectionTimeUtc = nowUtc < anchorUtc ? anchorUtc : nowUtc;
            TimeSpan regenerationInterval = TimeSpan.FromSeconds(regenerationIntervalSeconds);
            long completedIntervals = (projectionTimeUtc - anchorUtc).Ticks / regenerationInterval.Ticks;
            int displayedLives = (int)Math.Min(maximumLives, availableLives + completedIntervals);
            if (displayedLives == maximumLives) return new PlayerLivesViewData(displayedLives, maximumLives, null);

            long elapsedIntervalTicks = (projectionTimeUtc - anchorUtc).Ticks % regenerationInterval.Ticks;
            TimeSpan regenerationTimeRemaining = TimeSpan.FromTicks(regenerationInterval.Ticks - elapsedIntervalTicks);
            return new PlayerLivesViewData(displayedLives, maximumLives, regenerationTimeRemaining);
        }
    }
}
