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
        private Vector3 restingScale;
        private float restingAlpha;

        private void Awake()
        {
            if (scaleTarget != null)
                restingScale = scaleTarget.localScale;
            if (fadeTarget != null)
                restingAlpha = fadeTarget.alpha;
        }

        private void OnEnable()
        {
            PlayEntrance().Forget();
        }

        private void OnDisable()
        {
            CancelActiveEntrance();
        }

        public UniTask PlayEntrance()
        {
            CancelActiveEntrance();
            entranceCompletionSource = new UniTaskCompletionSource();
            entranceSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            Sequence activeSequence = entranceSequence;
            UniTaskCompletionSource activeCompletionSource = entranceCompletionSource;
            bool hasEntrance = false;

            if (scaleTarget != null)
            {
                // Never use zero scale here; it breaks ScrollRect bounds
                scaleTarget.localScale = restingScale * 0.01f;
                entranceSequence.Join(scaleTarget.DOScale(restingScale, duration).SetEase(Ease.OutBack));
                hasEntrance = true;
            }

            if (fadeTarget != null)
            {
                fadeTarget.alpha = 0f;
                entranceSequence.Join(fadeTarget.DOFade(restingAlpha, duration).SetEase(Ease.OutQuad));
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
            entranceSequence.OnKill(() => HandleEntranceKilled(activeSequence, activeCompletionSource));

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

        private void CancelActiveEntrance()
        {
            Sequence activeSequence = entranceSequence;
            UniTaskCompletionSource activeCompletionSource = entranceCompletionSource;
            entranceSequence = null;
            activeSequence?.Kill();
            RestoreRestingState();

            if (activeSequence != null)
                activeCompletionSource.TrySetCanceled();
        }

        private void HandleEntranceKilled(Sequence activeSequence, UniTaskCompletionSource activeCompletionSource)
        {
            if (entranceSequence != activeSequence) return;

            entranceSequence = null;
            RestoreRestingState();
            activeCompletionSource.TrySetCanceled();
        }

        private void RestoreRestingState()
        {
            if (scaleTarget != null)
                scaleTarget.localScale = restingScale;
            if (fadeTarget != null)
                fadeTarget.alpha = restingAlpha;
        }
    }
}
