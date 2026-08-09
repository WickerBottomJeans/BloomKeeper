using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _overlayRenderer;
        [SerializeField] private SpriteRenderer _overlayAnimationRenderer;
        private readonly Dictionary<SpriteRenderer, Vector3> _targetScales = new();

        public bool HasOverlay => _overlayRenderer.sprite != null;

        private float _tileSize;
        private Vector3 _restingLocalPosition;
        private Material _normalBaseMaterial;
        private bool _boosterTargetShown;

        public void Init(float tileSize, int baseSortingOrder)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize));

            _tileSize = tileSize;
            _restingLocalPosition = transform.localPosition;
            _normalBaseMaterial = _baseRenderer.sharedMaterial;
            _baseRenderer.sortingOrder = baseSortingOrder;
            _overlayAnimationRenderer.gameObject.SetActive(false);
        }

        public void SetBase(Sprite sprite)
        {
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            _baseRenderer.sprite = sprite;
            Vector3 scale = Vector3.one * (_tileSize / sprite.bounds.size.x);
            _baseRenderer.transform.localScale = scale;
            _targetScales[_baseRenderer] = scale;
        }

        public void ShowBoosterTarget(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (_boosterTargetShown) throw new InvalidOperationException("Booster target presentation is already shown on this tile.");

            _boosterTargetShown = true;
            _baseRenderer.sharedMaterial = material;
        }

        public void HideBoosterTarget()
        {
            if (!_boosterTargetShown) throw new InvalidOperationException("Booster target presentation is not shown on this tile.");

            _baseRenderer.sharedMaterial = _normalBaseMaterial;
            _boosterTargetShown = false;
        }

        public void SetOverlay(Sprite sprite)
        {
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            _overlayRenderer.sprite = sprite;
            FitToTile(_overlayRenderer, sprite);
        }

        public void ClearOverlay()
        {
            _overlayRenderer.sprite = null;
        }

        public async UniTask PlayOverlayTransition(Sprite incoming)
        {
            PrepareOverlayAnimation(_overlayRenderer.sprite);

            SetOverlay(incoming);
            _overlayRenderer.color = new Color(1f, 1f, 1f, 0f);
            _overlayRenderer.transform.localScale = Vector3.zero;

            Vector3 outgoingTargetScale = GetTargetScale(_overlayAnimationRenderer);
            Vector3 incomingTargetScale = GetTargetScale(_overlayRenderer);

            Sequence outgoing = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            Tween outgoingScale = _overlayAnimationRenderer.transform.DOScale(outgoingTargetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad);
            Tween outgoingFade = _overlayAnimationRenderer.DOFade(0f, 0.15f);
            _ = outgoing.Append(outgoingScale);
            _ = outgoing.Join(outgoingFade);

            Sequence incomingSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            Tween incomingScale = _overlayRenderer.transform.DOScale(incomingTargetScale, 0.2f).SetEase(Ease.OutBack);
            Tween incomingFade = _overlayRenderer.DOFade(1f, 0.2f);
            _ = incomingSequence.Append(incomingScale);
            _ = incomingSequence.Join(incomingFade);

            await UniTask.WhenAll(outgoing.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()), incomingSequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));

            ClearOverlayAnimation();
        }

        public UniTask PlayOverlaySpawn()
        {
            Vector3 targetScale = GetTargetScale(_overlayRenderer);
            _overlayRenderer.color = new Color(1f, 1f, 1f, 0f);
            _overlayRenderer.transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(_overlayRenderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack));
            sequence.Join(_overlayRenderer.DOFade(1f, 0.2f));
            return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayOverlayDespawn()
        {
            Vector3 targetScale = GetTargetScale(_overlayRenderer);

            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(_overlayRenderer.transform.DOScale(targetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));
            sequence.Join(_overlayRenderer.DOFade(0f, 0.15f));
            return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public UniTask PlayRipple(Vector2 displacement, float delay, float duration)
        {
            if (delay < 0f) throw new ArgumentOutOfRangeException(nameof(delay));
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));

            transform.DOKill();
            transform.localPosition = _restingLocalPosition;

            float legDuration = duration * 0.5f;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.SetTarget(transform);
            sequence.AppendInterval(delay);
            sequence.Append(transform.DOLocalMove(_restingLocalPosition + (Vector3)displacement, legDuration).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOLocalMove(_restingLocalPosition, legDuration).SetEase(Ease.OutBack));
            sequence.OnKill(() =>
            {
                if (this != null)
                    transform.localPosition = _restingLocalPosition;
            });
            return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        private void PrepareOverlayAnimation(Sprite outgoingSprite)
        {
            _overlayAnimationRenderer.sprite = outgoingSprite;
            FitToTile(_overlayAnimationRenderer, outgoingSprite);
            _overlayAnimationRenderer.gameObject.SetActive(true);
        }

        private void ClearOverlayAnimation()
        {
            _overlayAnimationRenderer.sprite = null;
            _overlayAnimationRenderer.gameObject.SetActive(false);
        }

        private void FitToTile(SpriteRenderer renderer, Sprite sprite)
        {
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            Vector3 scale = Vector3.one * (_tileSize / spriteSize);
            renderer.transform.localScale = scale;
            _targetScales[renderer] = scale;
        }

        private Vector3 GetTargetScale(SpriteRenderer renderer) => _targetScales[renderer];
    }
}
