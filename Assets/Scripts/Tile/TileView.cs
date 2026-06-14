using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _overlayRenderer;

        private float _cellSize;

        public void Init(float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            _cellSize = cellSize;
        }

        public void SetBase(Sprite sprite)
        {
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            _baseRenderer.sprite = sprite;
            FitToCell(_baseRenderer, sprite);
        }

        public void SetOverlay(Sprite sprite)
        {
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            _overlayRenderer.sprite = sprite;
            FitToCell(_overlayRenderer, sprite);
        }

        public void ClearOverlay()
        {
            _overlayRenderer.sprite = null;
        }

        private void FitToCell(SpriteRenderer renderer, Sprite sprite)
        {
            float spriteSize = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y);

            renderer.transform.localScale = Vector3.one * (_cellSize / spriteSize);
        }
    }
}