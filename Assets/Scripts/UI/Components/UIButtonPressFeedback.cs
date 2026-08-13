using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private UIButtonPressFeedbackType feedbackType = UIButtonPressFeedbackType.Scale;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float pressDuration = 0.2f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.2f;
        [SerializeField] private Ease pressEase = Ease.OutQuad;
        [SerializeField] private Ease releaseEase = Ease.OutQuad;

        [Header("Scale")]
        [SerializeField, Min(0f)] private float pressedScale = 0.92f;

        [Header("Jelly")]
        [SerializeField] private Vector2 jellyPressedScale = new Vector2(1.08f, 0.88f);
        [SerializeField] private Vector2 jellyReleaseScale = new Vector2(0.94f, 1.08f);

        private Vector3 baseScale;
        private Tween feedbackTween;
        private bool isPressed;

        private void Awake()
        {
            baseScale = visualRoot.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;

            isPressed = true;
            PlayPressAnimation();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed) return;
            PlayReleaseAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPressed) return;
            PlayReleaseAnimation();
        }

        public void ResetImmediately()
        {
            isPressed = false;
            KillFeedbackTween();
            visualRoot.localScale = baseScale;
        }

        private void PlayPressAnimation()
        {
            KillFeedbackTween();
            Vector3 targetScale = feedbackType switch
            {
                UIButtonPressFeedbackType.Scale => baseScale * pressedScale,
                UIButtonPressFeedbackType.Jelly => ScaleBy(baseScale, jellyPressedScale),
                _ => throw new ArgumentOutOfRangeException(nameof(feedbackType), feedbackType, "Unsupported button press feedback type.")
            };
            feedbackTween = visualRoot.DOScale(targetScale, pressDuration).SetEase(pressEase).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void PlayReleaseAnimation()
        {
            isPressed = false;
            KillFeedbackTween();
            if (feedbackType == UIButtonPressFeedbackType.Scale)
            {
                feedbackTween = visualRoot.DOScale(baseScale, releaseDuration).SetEase(releaseEase).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                return;
            }
            if (feedbackType != UIButtonPressFeedbackType.Jelly)
                throw new ArgumentOutOfRangeException(nameof(feedbackType), feedbackType, "Unsupported button press feedback type.");

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(visualRoot.DOScale(ScaleBy(baseScale, jellyReleaseScale), releaseDuration * 0.5f).SetEase(releaseEase));
            sequence.Append(visualRoot.DOScale(baseScale, releaseDuration * 0.5f).SetEase(releaseEase));
            feedbackTween = sequence;
        }

        private  Vector3 ScaleBy(Vector3 scale, Vector2 multiplier)
        {
            return new Vector3(scale.x * multiplier.x, scale.y * multiplier.y, scale.z);
        }

        private void KillFeedbackTween()
        {
            if (feedbackTween == null) return;
            feedbackTween.Kill();
            feedbackTween = null;
        }

        private void OnDisable()
        {
            ResetImmediately();
        }

        private void OnDestroy()
        {
            KillFeedbackTween();
        }
    }
}
