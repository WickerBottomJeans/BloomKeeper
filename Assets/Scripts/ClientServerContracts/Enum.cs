namespace DefaultNamespace
{
    public enum RewardKind
    {
        InventoryItem = 1,
        Currency = 2,
        TimedEntitlement = 3
    }

    public enum ConsumeBoosterOutcome
    {
        Consumed = 1,
        Rejected = 2
    }

    public enum ConsumeBoosterRejectionReason
    {
        InsufficientQuantity = 1
    }

    public enum LevelAttemptStatus
    {
        Active = 1,
        Completed = 2,
        Abandoned = 3
    }

    public enum StartLevelAttemptOutcome
    {
        Approved = 1,
        Rejected = 2
    }

    public enum StartLevelAttemptRejectionReason
    {
        LevelLocked = 1,
        InsufficientLives = 2,
        OperationConflict = 3,
        LevelUnavailable = 4
    }

    public enum AbandonLevelAttemptOutcome
    {
        Abandoned = 1,
        Rejected = 2
    }

    public enum AbandonLevelAttemptRejectionReason
    {
        AttemptNotCurrent = 1,
        AttemptAlreadyCompleted = 2
    }

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
        AttemptResultConflict = 5,
        AttemptNotCurrent = 6,
        AttemptNotActive = 7,
        AttemptLevelMismatch = 8
    }
}
