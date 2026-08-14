using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class CompleteLevelAttemptResponse
{
    public CompleteLevelAttemptOutcome outcome;
    public CompleteLevelAttemptRejectionReason? rejectionReason;
    public int levelId;
    public LevelProgressData levelProgress;
    public int highestUnlockedLevel;
    public PlayerLivesSnapshot lives;
}
