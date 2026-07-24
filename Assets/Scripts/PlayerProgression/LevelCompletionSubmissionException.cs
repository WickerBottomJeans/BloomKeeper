using System;

namespace DefaultNamespace
{
    public sealed class LevelCompletionSubmissionException : Exception
    {
        public bool IsRetryable { get; }
        public uint? RetryAfterSeconds { get; }

        public LevelCompletionSubmissionException(string message, bool isRetryable, uint? retryAfterSeconds = null) : base(message)
        {
            IsRetryable = isRetryable;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
