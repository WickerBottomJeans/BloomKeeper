using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIPopupEntranceAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private CanvasGroup fadeTarget;
        [SerializeField] private float duration = 0.2f;

        private Sequence entranceSequence;
        private UniTaskCompletionSource entranceCompletionSource;

        private void OnEnable()
        {
            PlayEntrance().Forget();
        }

        public UniTask PlayEntrance()
        {
            entranceSequence?.Kill();
            entranceCompletionSource = new UniTaskCompletionSource();
            entranceSequence = DOTween.Sequence();
            Sequence activeSequence = entranceSequence;
            UniTaskCompletionSource activeCompletionSource = entranceCompletionSource;
            bool hasEntrance = false;

            if (scaleTarget != null)
            {
                Vector3 targetScale = scaleTarget.localScale;
                scaleTarget.localScale = Vector3.zero;
                entranceSequence.Join(scaleTarget.DOScale(targetScale, duration).SetEase(Ease.OutBack));
                hasEntrance = true;
            }

            if (fadeTarget != null)
            {
                float targetAlpha = fadeTarget.alpha;
                fadeTarget.alpha = 0f;
                entranceSequence.Join(fadeTarget.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad));
                hasEntrance = true;
            }

            if (!hasEntrance)
            {
                entranceSequence.Kill();
                entranceSequence = null;
                activeCompletionSource.TrySetResult();
                return activeCompletionSource.Task;
            }

            entranceSequence.OnComplete(() => CompleteEntrance(activeSequence, activeCompletionSource));
            entranceSequence.OnKill(() => CompleteEntrance(activeSequence, activeCompletionSource));

            return WaitForEntrance();
        }

        public UniTask WaitForEntrance()
        {
            return entranceCompletionSource?.Task ?? UniTask.CompletedTask;
        }

        private void CompleteEntrance(Sequence activeSequence, UniTaskCompletionSource activeCompletionSource)
        {
            if (entranceSequence != activeSequence) return;

            entranceSequence = null;
            activeCompletionSource.TrySetResult();
        }
    }
}
