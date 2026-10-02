using System;
using System.Threading;
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
using UnityEngine;
#endif

namespace DefaultNamespace.Reviews
{
    public class GooglePlayReviewService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private ReviewManager reviewManager;

        public bool IsGooglePlayReviewSupported => true;

        /// <summary>
        /// Requests a native review flow. Completion does not confirm that a dialog appeared or a review was submitted.
        /// </summary>
        public async UniTask<bool> TryRequestAppReviewAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reviewManager ??= new ReviewManager();

            var requestReviewOperation = reviewManager.RequestReviewFlow();
            await UniTask.WaitUntil(() => requestReviewOperation.IsDone, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (requestReviewOperation.Error != ReviewErrorCode.NoError)
            {
                Debug.LogWarning($"Google Play review request failed: {requestReviewOperation.Error}.");
                return false;
            }

            PlayReviewInfo playReviewInfo = requestReviewOperation.GetResult();
            var launchReviewOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
            await UniTask.WaitUntil(() => launchReviewOperation.IsDone);
            if (launchReviewOperation.Error != ReviewErrorCode.NoError)
            {
                Debug.LogWarning($"Google Play review launch failed: {launchReviewOperation.Error}.");
                return false;
            }

            return true;
        }
#else
        public bool IsGooglePlayReviewSupported => false;

        /// <summary>
        /// Native Google Play reviews require an Android player.
        /// </summary>
        public UniTask<bool> TryRequestAppReviewAsync(CancellationToken cancellationToken = default)
        {
            return UniTask.FromException<bool>(new PlatformNotSupportedException("Google Play reviews require an Android player outside the Unity Editor."));
        }
#endif
    }
}
