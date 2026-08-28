using System;

namespace DefaultNamespace
{
    public static class PlayerLivesContract
    {
        public const int CurrentSchemaVersion = 1;

        public static void ValidateSnapshot(PlayerLivesSnapshot lives)
        {
            if (lives == null) throw new ArgumentNullException(nameof(lives));
            if (lives.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Lives snapshot has unsupported schema version {lives.schemaVersion}.");
            if (lives.maximumLives <= 0) throw new InvalidOperationException("Lives snapshot maximumLives must be greater than zero.");
            if (lives.availableLives < 0 || lives.availableLives > lives.maximumLives) throw new InvalidOperationException($"Lives snapshot has invalid availableLives {lives.availableLives}.");
            if (lives.regenerationIntervalSeconds <= 0) throw new InvalidOperationException("Lives snapshot regenerationIntervalSeconds must be greater than zero.");
            if (lives.availableLives == lives.maximumLives && lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("A full lives snapshot cannot have a regeneration anchor.");
            if (lives.availableLives < lives.maximumLives && !lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("A lives snapshot below the cap must have a regeneration anchor.");
        }
    }

    public class PlayerLivesSnapshot
    {
        public int schemaVersion;
        public int availableLives;
        public int maximumLives;
        public int regenerationIntervalSeconds;
        public DateTimeOffset? regenerationAnchorUtc;
        public DateTimeOffset? unlimitedLivesExpiresAtUtc;
    }
}
