namespace DefaultNamespace
{
    public class CompleteLevelAttemptResponse
    {
        public CompleteLevelAttemptOutcome outcome;
        public CompleteLevelAttemptRejectionReason? rejectionReason;
        public int levelId;
        public LevelProgressData levelProgress;
        public int highestUnlockedLevel;
    }
}
