using BloomKeeper.PlayFabFunctions.Models;

namespace BloomKeeper.PlayFabFunctions.Services;

public class CompleteLevelAttemptService
{
    public (CompleteLevelAttemptResponse response, bool progressionChanged) Apply(PlayerProgressionData playerProgression, CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        if (playerProgression == null) throw new System.ArgumentNullException(nameof(playerProgression));
        if (completeLevelAttemptRequest == null) throw new System.ArgumentNullException(nameof(completeLevelAttemptRequest));

        if (!System.Guid.TryParseExact(completeLevelAttemptRequest.attemptId, "N", out System.Guid parsedAttemptId))
            return (CreateRejectedResponse(playerProgression, completeLevelAttemptRequest.levelId, CompleteLevelAttemptRejectionReason.InvalidAttemptId), false);

        string canonicalAttemptId = parsedAttemptId.ToString("N");
        if (playerProgression.processedLevelAttempts.TryGetValue(canonicalAttemptId, out ProcessedLevelAttemptData processedAttempt))
        {
            if (!Matches(processedAttempt, completeLevelAttemptRequest))
                return (CreateRejectedResponse(playerProgression, completeLevelAttemptRequest.levelId, CompleteLevelAttemptRejectionReason.AttemptIdConflict), false);

            return (CreateSavedResponse(playerProgression, completeLevelAttemptRequest.levelId), false);
        }

        CompleteLevelAttemptRejectionReason? rejectionReason = GetRejectionReason(playerProgression, completeLevelAttemptRequest);
        if (rejectionReason.HasValue)
            return (CreateRejectedResponse(playerProgression, completeLevelAttemptRequest.levelId, rejectionReason.Value), false);

        playerProgression.levels.TryGetValue(completeLevelAttemptRequest.levelId, out LevelProgressData levelProgress);
        levelProgress ??= new LevelProgressData();

        if (completeLevelAttemptRequest.didWin)
        {
            levelProgress.completed = true;
            levelProgress.bestStars = System.Math.Max(levelProgress.bestStars, completeLevelAttemptRequest.stars);
            levelProgress.bestScore = System.Math.Max(levelProgress.bestScore, completeLevelAttemptRequest.score);
            playerProgression.highestUnlockedLevel = System.Math.Max(playerProgression.highestUnlockedLevel, completeLevelAttemptRequest.levelId + 1);
            playerProgression.levels[completeLevelAttemptRequest.levelId] = levelProgress;
        }

        playerProgression.processedLevelAttempts[canonicalAttemptId] = new ProcessedLevelAttemptData { levelId = completeLevelAttemptRequest.levelId, didWin = completeLevelAttemptRequest.didWin, score = completeLevelAttemptRequest.score, stars = completeLevelAttemptRequest.stars };
        return (CreateSavedResponse(playerProgression, completeLevelAttemptRequest.levelId), true);
    }

    private static bool Matches(ProcessedLevelAttemptData processedAttempt, CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        return processedAttempt.levelId == completeLevelAttemptRequest.levelId && processedAttempt.didWin == completeLevelAttemptRequest.didWin && processedAttempt.score == completeLevelAttemptRequest.score && processedAttempt.stars == completeLevelAttemptRequest.stars;
    }

    private static CompleteLevelAttemptResponse CreateSavedResponse(PlayerProgressionData playerProgression, int levelId)
    {
        playerProgression.levels.TryGetValue(levelId, out LevelProgressData levelProgress);
        levelProgress ??= new LevelProgressData();
        return new CompleteLevelAttemptResponse { outcome = CompleteLevelAttemptOutcome.Saved, levelId = levelId, levelProgress = levelProgress, highestUnlockedLevel = playerProgression.highestUnlockedLevel };
    }

    private static CompleteLevelAttemptResponse CreateRejectedResponse(PlayerProgressionData playerProgression, int levelId, CompleteLevelAttemptRejectionReason rejectionReason)
    {
        return new CompleteLevelAttemptResponse { outcome = CompleteLevelAttemptOutcome.Rejected, rejectionReason = rejectionReason, levelId = levelId, highestUnlockedLevel = playerProgression.highestUnlockedLevel };
    }

    private static CompleteLevelAttemptRejectionReason? GetRejectionReason(PlayerProgressionData playerProgression, CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        // TODO: Validate levelId against the server-owned online level catalog when remote level content is implemented.
        if (completeLevelAttemptRequest.levelId > playerProgression.highestUnlockedLevel) return CompleteLevelAttemptRejectionReason.LevelLocked;
        if (completeLevelAttemptRequest.stars < 0) return CompleteLevelAttemptRejectionReason.NegativeStars;
        if (completeLevelAttemptRequest.score < 0) return CompleteLevelAttemptRejectionReason.NegativeScore;
        return null;
    }
}
