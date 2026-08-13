using System;

namespace DefaultNamespace
{
    public class BoosterConsumptionException : Exception
    {
        public bool IsRetryable { get; }

        public BoosterConsumptionException(string message, bool isRetryable) : base(message)
        {
            IsRetryable = isRetryable;
        }
    }
}
