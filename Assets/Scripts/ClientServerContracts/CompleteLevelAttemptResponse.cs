using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Server response after the client asks to complete a level attempt.
    /// </summary>
    public class CompleteLevelAttemptResponse
    {
        public CompleteLevelAttemptOutcome outcome;
        public CompleteLevelAttemptRejectionReason? rejectionReason;
        public int levelId;
        public LevelProgressData levelProgress;
        public int highestUnlockedLevel;
        public PlayerLivesSnapshot lives;
        public PlayerInventorySnapshot playerInventorySnapshot;
        public List<string> completionRewardPresentationKeys = new List<string>();
    }
}
