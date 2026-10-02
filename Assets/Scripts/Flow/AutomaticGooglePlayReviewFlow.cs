using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Reviews;
using DefaultNamespace.Settings;

namespace DefaultNamespace
{
    public class AutomaticGooglePlayReviewFlow
    {
        private const int ReviewMilestoneChapterId = 1;
        private readonly GooglePlayReviewService googlePlayReviewService = new();

        /// <summary>
        /// Requests a review once on this installation after Chapter 1 is complete.
        /// </summary>
        public async UniTask TryRequestAutomaticGooglePlayReviewAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!googlePlayReviewService.IsGooglePlayReviewSupported || PlayerPrefsStore.LoadAutomaticGooglePlayReviewAttempted()) return;

            ChapterDefinition chapterDefinition = await ConfigManager.Instance.GetChapterDefinitionAsync(ReviewMilestoneChapterId).AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PlayerProgressionData playerProgressionData = PlayerAccountContext.Instance.CurrentAccount.Progression;
            foreach (ChapterLevelDisplayData chapterLevelDisplayData in chapterDefinition.levels)
            {
                if (!playerProgressionData.levels.TryGetValue(chapterLevelDisplayData.levelId, out LevelProgressData levelProgressData) || !levelProgressData.completed) return;
            }

            // Record the automatic review attempt.
            PlayerPrefsStore.SaveAutomaticGooglePlayReviewAttempted();
            await googlePlayReviewService.TryRequestAppReviewAsync(cancellationToken);
        }
    }
}
