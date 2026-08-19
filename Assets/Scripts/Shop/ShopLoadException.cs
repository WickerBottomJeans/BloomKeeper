using System;

namespace DefaultNamespace
{
    public class ShopLoadException : Exception
    {
        public bool IsRetryable { get; }
        public uint? RetryAfterSeconds { get; }

        public ShopLoadException(string message, bool isRetryable, uint? retryAfterSeconds = null) : base(message)
        {
            IsRetryable = isRetryable;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
