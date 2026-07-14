using BloomKeeper.PlayFabFunctions.Models;

namespace BloomKeeper.PlayFabFunctions.Services;

public class CompleteLevelAttemptService
{
    //TODO: maybe add anti cheat later
    public bool SanityCheck(PlayerProgressionData playerProgression, CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        if (playerProgression == null) return false;
        if (completeLevelAttemptRequest == null) return false;
        if (completeLevelAttemptRequest.levelId > playerProgression.highestUnlockedLevel) return false;
        if (completeLevelAttemptRequest.stars < 0) return false;
        if (completeLevelAttemptRequest.score < 0) return false;

        return true;
    }
    
    public CompleteLevelAttemptResponse Apply(PlayerProgressionData playerProgression, CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        if (!SanityCheck(playerProgression, completeLevelAttemptRequest)) throw new System.InvalidOperationException("CompleteLevelAttempt request failed sanity check.");

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
        return new CompleteLevelAttemptResponse { levelId = completeLevelAttemptRequest.levelId, levelProgress = levelProgress, highestUnlockedLevel = playerProgression.highestUnlockedLevel };
    }
}
