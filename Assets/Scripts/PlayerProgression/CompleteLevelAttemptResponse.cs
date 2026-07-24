namespace DefaultNamespace
{
    public enum CompleteLevelAttemptOutcome
    {
        Saved = 1,
        Rejected = 2
    }

    public enum CompleteLevelAttemptRejectionReason
    {
        LevelLocked = 1,
        NegativeStars = 2,
        NegativeScore = 3,
        InvalidAttemptId = 4,
        AttemptIdConflict = 5
    }

    public class CompleteLevelAttemptResponse
    {
        public CompleteLevelAttemptOutcome outcome;
        public CompleteLevelAttemptRejectionReason? rejectionReason;
        public int levelId;
        public LevelProgressData levelProgress;
        public int highestUnlockedLevel;
    }
}
