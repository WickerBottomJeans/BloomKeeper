using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// [Duong] Owns life regeneration, spending, and refunds.
/// </summary>
public class LivesService
{
    /// <summary>
    /// [Duong] Apply the lives rules for a new level attempt.
    /// </summary>
    public bool TryHandleNewLevelAttempt(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        RegenerateLives(lives, config, now);
        if (lives.availableLives == 0) return false;

        if (lives.availableLives == config.maximumLives) lives.regenerationAnchorUtc = now;
        lives.availableLives--;
        return true;
    }

    /// <summary>
    /// [Duong] Apply the lives rules for an ended level attempt.
    /// </summary>
    public void HandleLevelAttemptEnded(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now, bool didWin)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        RegenerateLives(lives, config, now);
        if (!didWin) return;
        if (lives.availableLives == config.maximumLives) return;

        lives.availableLives++;
        if (lives.availableLives == config.maximumLives) lives.regenerationAnchorUtc = null;
    }

    /// <summary>
    /// [Duong] Calculates regenerated lives and updates the PlayerLivesData.
    /// </summary>
    public bool RegenerateLives(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now)
    {
        //[Duong] Check required data.
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        //[Duong] Stop regeneration when the player already has the maximum number of lives.
        if (lives.availableLives == config.maximumLives)
        {
            lives.regenerationAnchorUtc = null;
            return false;
        }

        //[Duong] Check the regeneration anchor.
        if (!lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("Lives below the cap are missing their regeneration anchor.");
        DateTimeOffset regenerationAnchorUtc = lives.regenerationAnchorUtc.Value;
        if (regenerationAnchorUtc > now) throw new InvalidOperationException("The lives regeneration anchor is later than the current server time.");

        //[Duong] Calculate regenerated lives and update the regeneration anchor.
        TimeSpan regenerationInterval = TimeSpan.FromSeconds(config.regenerationIntervalSeconds);
        long completedIntervals = (now - regenerationAnchorUtc).Ticks / regenerationInterval.Ticks;
        if (completedIntervals == 0) return false;

        int missingLives = config.maximumLives - lives.availableLives;
        int regeneratedLives = (int)Math.Min(completedIntervals, missingLives);
        lives.availableLives += regeneratedLives;

        if (lives.availableLives == config.maximumLives)
            lives.regenerationAnchorUtc = null;
        else
            lives.regenerationAnchorUtc = regenerationAnchorUtc.AddTicks(regenerationInterval.Ticks * regeneratedLives);
        return true;
    }

    public PlayerLivesSnapshot CreateLivesSnapshot(PlayerLivesData lives, PlayerLivesConfig config)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        return new PlayerLivesSnapshot
        {
            schemaVersion = PlayerLivesContract.CurrentSchemaVersion,
            availableLives = lives.availableLives,
            maximumLives = config.maximumLives,
            regenerationIntervalSeconds = config.regenerationIntervalSeconds,
            regenerationAnchorUtc = lives.regenerationAnchorUtc
        };
    }
}
