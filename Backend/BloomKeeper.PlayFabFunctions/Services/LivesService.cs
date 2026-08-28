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
    public bool TryHandleNewLevelAttempt(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now, out bool didSpendLife)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        // [Duong] Regenerate regular lives to the current server time.
        UpdateLivesToCurrentTime(lives, config, now);

        // [Duong] Let active unlimited lives cover the attempt.
        if (lives.unlimitedLivesExpiresAtUtc.HasValue && now < lives.unlimitedLivesExpiresAtUtc.Value)
        {
            didSpendLife = false;
            return true;
        }

        // [Duong] Reject when no regular life is available.
        if (lives.availableLives == 0)
        {
            didSpendLife = false;
            return false;
        }

        // [Duong] Spend one regular life.
        if (lives.availableLives == config.maximumLives) lives.regenerationAnchorUtc = now;
        lives.availableLives--;
        didSpendLife = true;
        return true;
    }

    /// <summary>
    /// [Duong] Apply the lives rules for an ended level attempt.
    /// </summary>
    public void HandleLevelAttemptEnded(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now, bool didWin, bool didSpendLife)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        UpdateLivesToCurrentTime(lives, config, now);
        if (!didWin || !didSpendLife) return;
        if (lives.availableLives == config.maximumLives) return;

        lives.availableLives++;
        if (lives.availableLives == config.maximumLives) lives.regenerationAnchorUtc = null;
    }

    /// <summary>
    /// Adds unlimited lives time after updating lives to the current server time.
    /// </summary>
    public void GrantUnlimitedLives(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now, int durationSeconds)
    {
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Unlimited lives duration must be greater than zero.");

        UpdateLivesToCurrentTime(lives, config, now);
        DateTimeOffset unlimitedLivesStartUtc = lives.unlimitedLivesExpiresAtUtc.HasValue && lives.unlimitedLivesExpiresAtUtc.Value > now ? lives.unlimitedLivesExpiresAtUtc.Value : now;
        lives.unlimitedLivesExpiresAtUtc = unlimitedLivesStartUtc.AddSeconds(durationSeconds);
    }

    /// <summary>
    /// [Duong] Removes previously granted unlimited lives time.
    /// </summary>
    public void SubtractUnlimitedLivesDuration(PlayerLivesData playerLivesData, DateTimeOffset operationTimeUtc, int durationSeconds)
    {
        if (playerLivesData == null) throw new ArgumentNullException(nameof(playerLivesData));
        if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Unlimited lives duration must be greater than zero.");
        if (!playerLivesData.unlimitedLivesExpiresAtUtc.HasValue) throw new InvalidOperationException("Unlimited lives cannot be reverted because no unlimited lives expiration exists.");

        DateTimeOffset revertedUnlimitedLivesExpiresAtUtc = playerLivesData.unlimitedLivesExpiresAtUtc.Value.AddSeconds(-durationSeconds);
        playerLivesData.unlimitedLivesExpiresAtUtc = revertedUnlimitedLivesExpiresAtUtc > operationTimeUtc ? revertedUnlimitedLivesExpiresAtUtc : null;
    }
    
    /// <summary>
    /// [Duong] Calculates regenerated lives and updates the PlayerLivesData.
    /// </summary>
    /// <returns>did update lives to the current time change the saved data</returns>
    public bool UpdateLivesToCurrentTime(PlayerLivesData lives, PlayerLivesConfig config, DateTimeOffset now)
    {
        //[Duong] Check required data.
        if (lives == null) throw new ArgumentNullException(nameof(lives));
        if (config == null) throw new ArgumentNullException(nameof(config));

        bool livesChanged = false;
        if (lives.unlimitedLivesExpiresAtUtc.HasValue && lives.unlimitedLivesExpiresAtUtc.Value <= now)
        {
            lives.unlimitedLivesExpiresAtUtc = null;
            livesChanged = true;
        }

        //[Duong] Stop regeneration when the player already has the maximum number of lives.
        if (lives.availableLives == config.maximumLives)
        {
            lives.regenerationAnchorUtc = null;
            return livesChanged;
        }

        //[Duong] Check the regeneration anchor.
        if (!lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("Lives below the cap are missing their regeneration anchor.");
        DateTimeOffset regenerationAnchorUtc = lives.regenerationAnchorUtc.Value;
        if (regenerationAnchorUtc > now) throw new InvalidOperationException("The lives regeneration anchor is later than the current server time.");

        //[Duong] Calculate regenerated lives and update the regeneration anchor.
        TimeSpan regenerationInterval = TimeSpan.FromSeconds(config.regenerationIntervalSeconds);
        long completedIntervals = (now - regenerationAnchorUtc).Ticks / regenerationInterval.Ticks;
        if (completedIntervals == 0) return livesChanged;

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
            regenerationAnchorUtc = lives.regenerationAnchorUtc,
            unlimitedLivesExpiresAtUtc = lives.unlimitedLivesExpiresAtUtc
        };
    }
}
