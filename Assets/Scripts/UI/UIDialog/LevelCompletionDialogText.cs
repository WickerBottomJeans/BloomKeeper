using System;

namespace DefaultNamespace.UI
{
    public static class LevelCompletionDialogText
    {
        public const string RetryTitle = "Oops!";
        public const string RetryMessage = "We couldn't confirm your result right now. It's still here, so check your connection and try again.";
        public const string BackendFailureTitle = "Oops!";
        public const string BackendFailureMessage = "We couldn't confirm this result. Sorry about that—please return home and try the level again.";
        public const string RejectionTitle = "Oops!";

        public static string GetRejectionMessage(CompleteLevelAttemptRejectionReason rejectionReason)
        {
            switch (rejectionReason)
            {
                case CompleteLevelAttemptRejectionReason.LevelLocked:
                    return "This level isn't unlocked yet, so we couldn't save the result.";
                case CompleteLevelAttemptRejectionReason.NegativeStars:
                    return "That star count doesn't look right, so we couldn't save the result.";
                case CompleteLevelAttemptRejectionReason.NegativeScore:
                    return "That score doesn't look right, so we couldn't save the result.";
                case CompleteLevelAttemptRejectionReason.InvalidAttemptId:
                    return "This level attempt couldn't be identified, so we couldn't save the result.";
                case CompleteLevelAttemptRejectionReason.AttemptResultConflict:
                    return "This level attempt conflicts with a result that was already processed, so we couldn't save it.";
                case CompleteLevelAttemptRejectionReason.AttemptNotCurrent:
                    return "Someone else is playing on this account.";
                case CompleteLevelAttemptRejectionReason.AttemptNotActive:
                    return "This level attempt is no longer active, so we couldn't save the result.";
                case CompleteLevelAttemptRejectionReason.AttemptLevelMismatch:
                    return "This level doesn't match the active attempt, so we couldn't save the result.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rejectionReason), rejectionReason, "Unsupported level completion rejection reason.");
            }
        }
    }
}
