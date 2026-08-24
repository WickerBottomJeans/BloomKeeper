using System;

namespace DefaultNamespace
{
    public class PlayFabRequestException : Exception
    {
        public bool IsRetryable { get; }
        public uint? RetryAfterSeconds { get; }

        public PlayFabRequestException(string message, bool isRetryable, uint? retryAfterSeconds = null) : base(message)
        {
            IsRetryable = isRetryable;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
