using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIJawCurtain : MonoBehaviour
    {
        [SerializeField] private RectTransform upperJaw;
        [SerializeField] private RectTransform lowerJaw;
        [SerializeField] private TextMeshProUGUI tipLabel;
        [SerializeField] private Image tipImage;
        [SerializeField] private float duration = 0.4f;
        private Sequence sequence;

        public UniTask Close(string tipText = "", Sprite tipSprite = null)
        {
            KillSequence();
            SetTip(tipText, tipSprite);

            GetClosedPositions(out float upperY, out float lowerY);
            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(upperJaw.DOAnchorPosY(upperY, duration).SetEase(Ease.InOutCubic));
            sequence.Join(lowerJaw.DOAnchorPosY(lowerY, duration).SetEase(Ease.InOutCubic));
            return PlaySequence(sequence);
        }

        public UniTask Open()
        {
            KillSequence();

            GetOpenPositions(out float upperY, out float lowerY);
            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(upperJaw.DOAnchorPosY(upperY, duration).SetEase(Ease.InOutCubic));
            sequence.Join(lowerJaw.DOAnchorPosY(lowerY, duration).SetEase(Ease.InOutCubic));
            return PlaySequence(sequence);
        }

        public void SnapOpen()
        {
            KillSequence();
            GetOpenPositions(out float upperY, out float lowerY);
            upperJaw.anchoredPosition = new Vector2(upperJaw.anchoredPosition.x, upperY);
            lowerJaw.anchoredPosition = new Vector2(lowerJaw.anchoredPosition.x, lowerY);
        }

        public void SnapClosed(string tipText, Sprite tipSprite = null)
        {
            KillSequence();
            SetTip(tipText, tipSprite);
            GetClosedPositions(out float upperY, out float lowerY);
            upperJaw.anchoredPosition = new Vector2(upperJaw.anchoredPosition.x, upperY);
            lowerJaw.anchoredPosition = new Vector2(lowerJaw.anchoredPosition.x, lowerY);
        }

        private void SetTip(string tipText, Sprite tipSprite)
        {
            tipLabel.text = tipText;
            tipLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(tipText));
            tipImage.sprite = tipSprite;
            tipImage.gameObject.SetActive(tipSprite != null);
        }

        private void GetOpenPositions(out float upperY, out float lowerY)
        {
            RectTransform root = (RectTransform)transform;
            upperY = GetAnchoredYForEdge(root, upperJaw, root.rect.yMax, false);
            lowerY = GetAnchoredYForEdge(root, lowerJaw, root.rect.yMin, true);
        }

        private void GetClosedPositions(out float upperY, out float lowerY)
        {
            RectTransform root = (RectTransform)transform;
            upperY = GetAnchoredYForEdge(root, upperJaw, root.rect.yMax, true);
            lowerY = GetAnchoredYForEdge(root, lowerJaw, root.rect.yMin, false);
        }

        private float GetAnchoredYForEdge(RectTransform root, RectTransform rect, float targetY, bool topEdge)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, rect);
            float currentY = topEdge ? bounds.max.y : bounds.min.y;
            return rect.anchoredPosition.y + targetY - currentY;
        }

        private void KillSequence()
        {
            if (sequence == null) return;

            sequence.Kill();
            sequence = null;
        }

        private async UniTask PlaySequence(Sequence activeSequence)
        {
            try
            {
                await activeSequence.ToUniTask();
            }
            finally
            {
                if (sequence == activeSequence)
                    sequence = null;
            }
        }
    }
}
