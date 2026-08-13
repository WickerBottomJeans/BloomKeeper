using System;

namespace DefaultNamespace
{
    public class LevelAttemptRequestException : Exception
    {
        public bool IsRetryable { get; }
        public uint? RetryAfterSeconds { get; }

        public LevelAttemptRequestException(string message, bool isRetryable, uint? retryAfterSeconds = null) : base(message)
        {
            IsRetryable = isRetryable;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
