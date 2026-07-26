using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class UIShake : MonoBehaviour
    {
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField, Min(0f)] private float strength = 10f;
        [SerializeField, Min(1)] private int vibrato = 10;
        [SerializeField, Range(0f, 180f)] private float randomness = 90f;

        private Tween shakeTween;
        private Vector2 restingAnchoredPosition;
        private bool hasRestingPosition;

        public void Play()
        {
            ResetShake();
            restingAnchoredPosition = shakeTarget.anchoredPosition;
            hasRestingPosition = true;
            shakeTween = shakeTarget.DOShakeAnchorPos(duration, strength, vibrato, randomness, false, true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            shakeTween.OnComplete(HandleShakeCompleted);
        }

        private void HandleShakeCompleted()
        {
            shakeTarget.anchoredPosition = restingAnchoredPosition;
            shakeTween = null;
            hasRestingPosition = false;
        }

        private void OnDisable()
        {
            ResetShake();
        }

        private void ResetShake()
        {
            shakeTween?.Kill();
            shakeTween = null;
            if (!hasRestingPosition) return;

            shakeTarget.anchoredPosition = restingAnchoredPosition;
            hasRestingPosition = false;
        }
    }
}
