using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public abstract class UIPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform mainContent;
        [SerializeField] private CanvasGroup dimmer;
        [SerializeField, Range(0.001f, 1f)] private float hiddenScaleMultiplier = 0.01f;
        [SerializeField, Min(0f)] private float entranceDuration = 0.2f;
        [SerializeField, Min(0f)] private float exitDuration = 0.15f;
        [SerializeField] private Ease entranceContentEase = Ease.OutBack;
        [SerializeField] private Ease entranceDimmerEase = Ease.OutQuad;
        [SerializeField] private Ease exitContentEase = Ease.InBack;
        [SerializeField] private Ease exitDimmerEase = Ease.InQuad;

        private Sequence popupAnimationSequence;
        private Vector3 restingMainContentScale;
        private float restingDimmerAlpha;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (mainContent == null) throw new MissingReferenceException($"{nameof(UIPopup)} on '{name}' requires a main content RectTransform.");

            restingMainContentScale = mainContent.localScale;
            if (dimmer != null) restingDimmerAlpha = dimmer.alpha;
        }

        protected virtual void OnDisable()
        {
            KillPopupAnimation();
            RestorePopupRestingState();
        }

        #endregion

        #region Public API

        public void Show()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            PlayPopupEntrance();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            KillPopupAnimation();
            popupAnimationSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            popupAnimationSequence.Join(mainContent.DOScale(restingMainContentScale * hiddenScaleMultiplier, exitDuration).SetEase(exitContentEase));
            if (dimmer != null) popupAnimationSequence.Join(dimmer.DOFade(0f, exitDuration).SetEase(exitDimmerEase));
            popupAnimationSequence.OnComplete(CompletePopupExit);
        }

        protected virtual void HandlePopupEntranceCompleted()
        {
        }

        protected virtual void HandlePopupExitCompleted()
        {
        }

        #endregion

        #region Private Methods

        private void PlayPopupEntrance()
        {
            KillPopupAnimation();
            mainContent.localScale = restingMainContentScale * hiddenScaleMultiplier;
            if (dimmer != null) dimmer.alpha = 0f;
            popupAnimationSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            popupAnimationSequence.Join(mainContent.DOScale(restingMainContentScale, entranceDuration).SetEase(entranceContentEase));
            if (dimmer != null) popupAnimationSequence.Join(dimmer.DOFade(restingDimmerAlpha, entranceDuration).SetEase(entranceDimmerEase));
            popupAnimationSequence.OnComplete(CompletePopupEntrance);
        }

        private void CompletePopupEntrance()
        {
            popupAnimationSequence = null;
            HandlePopupEntranceCompleted();
        }

        private void CompletePopupExit()
        {
            popupAnimationSequence = null;
            HandlePopupExitCompleted();
            gameObject.SetActive(false);
        }

        private void KillPopupAnimation()
        {
            Sequence activePopupAnimationSequence = popupAnimationSequence;
            popupAnimationSequence = null;
            activePopupAnimationSequence?.Kill();
        }

        private void RestorePopupRestingState()
        {
            mainContent.localScale = restingMainContentScale;
            if (dimmer != null) dimmer.alpha = restingDimmerAlpha;
        }

        #endregion
    }
}
