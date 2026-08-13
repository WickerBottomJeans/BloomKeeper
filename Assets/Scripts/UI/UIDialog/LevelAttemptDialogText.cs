using System;

namespace DefaultNamespace.UI
{
    public static class LevelAttemptDialogText
    {
        public const string RetryTitle = "Connection interrupted";
        public const string StartRetryMessage = "We couldn't start this level right now. Check your connection and try again.";
        public const string AbandonRetryMessage = "We couldn't close the level attempt right now. Check your connection and try again.";
        public const string RejectionTitle = "Unable to start level";
        public const string AbandonRejectionTitle = "Unable to close level";

        public static string GetStartRejectionMessage(StartLevelAttemptRejectionReason rejectionReason)
        {
            return rejectionReason switch
            {
                StartLevelAttemptRejectionReason.LevelLocked => "This level isn't unlocked yet.",
                StartLevelAttemptRejectionReason.OperationConflict => "This level start conflicts with an earlier request.",
                _ => throw new ArgumentOutOfRangeException(nameof(rejectionReason), rejectionReason, "Unsupported level start rejection reason.")
            };
        }

        public static string GetAbandonRejectionMessage(AbandonLevelAttemptRejectionReason rejectionReason)
        {
            return rejectionReason switch
            {
                AbandonLevelAttemptRejectionReason.AttemptNotCurrent => "That level attempt is no longer the current attempt.",
                AbandonLevelAttemptRejectionReason.AttemptAlreadyCompleted => "That level attempt has already been completed.",
                _ => throw new ArgumentOutOfRangeException(nameof(rejectionReason), rejectionReason, "Unsupported level abandon rejection reason.")
            };
        }
    }
}
