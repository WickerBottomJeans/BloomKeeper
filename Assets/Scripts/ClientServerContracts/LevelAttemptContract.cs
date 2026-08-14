namespace DefaultNamespace
{
    public static class LevelAttemptContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    /// <summary>
    /// [Duong] Client's request to start a level attempt
    /// </summary>
    public class StartLevelAttemptRequest
    {
        public string startLevelRequestIdempotencyKey;
        public int levelId;
    }

    /// <summary>
    /// The server's response to the client's request to start a level.
    /// </summary>
    public class StartLevelAttemptResponse
    {
        public int schemaVersion;
        public StartLevelAttemptOutcome outcome;
        public StartLevelAttemptRejectionReason? rejectionReason;
        public string levelAttemptId;
        public PlayerLivesSnapshot lives;
    }

    public class AbandonLevelAttemptRequest
    {
        public string levelAttemptId;
    }

    public class AbandonLevelAttemptResponse
    {
        public int schemaVersion;
        public AbandonLevelAttemptOutcome outcome;
        public AbandonLevelAttemptRejectionReason? rejectionReason;
    }
}
