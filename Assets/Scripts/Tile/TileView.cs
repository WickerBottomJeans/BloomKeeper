using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _overlayRenderer;
        [SerializeField] private SpriteRenderer _overlayAnimationRenderer;
        private readonly Dictionary<SpriteRenderer, Vector3> _targetScales = new();

        public SpriteRenderer OverlayRenderer => _overlayRenderer;
        public SpriteRenderer OverlayAnimationRenderer => _overlayAnimationRenderer;

        private float _tileSize;

        public void Init(float tileSize, int baseSortingOrder)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize));

            _tileSize = tileSize;
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

        public void PrepareOverlayAnimation(Sprite outgoingSprite)
        {
            _overlayAnimationRenderer.sprite = outgoingSprite;
            FitToTile(_overlayAnimationRenderer, outgoingSprite);
            _overlayAnimationRenderer.gameObject.SetActive(true);
        }

        public void ClearOverlayAnimation()
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

        public Vector3 GetTargetScale(SpriteRenderer renderer) => _targetScales[renderer];
    }
}
