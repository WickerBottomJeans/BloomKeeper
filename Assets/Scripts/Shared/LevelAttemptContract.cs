namespace DefaultNamespace
{
    public static class LevelAttemptContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    public class StartLevelAttemptRequest
    {
        public string operationId;
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
