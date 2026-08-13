using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIJellyText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField, Range(0f, 1f)] private float compressedScale = 0.9f;
        [SerializeField, Min(1f)] private float expandedScale = 1.12f;
        [SerializeField, Min(0f)] private float animationDuration = 0.3f;

        private Sequence changeSequence;
        private Vector3 restingScale;
        private bool hasDisplayedValue;

        private void Awake()
        {
            restingScale = label.rectTransform.localScale;
        }

        public void SetText(string value)
        {
            bool shouldAnimate = hasDisplayedValue && label.text != value;
            label.text = value;
            hasDisplayedValue = true;

            if (!shouldAnimate) return;

            changeSequence?.Kill();
            label.rectTransform.localScale = restingScale;

            changeSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            changeSequence.Append(label.rectTransform.DOScale(restingScale * compressedScale, animationDuration * 0.25f).SetEase(Ease.OutQuad));
            changeSequence.Append(label.rectTransform.DOScale(restingScale * expandedScale, animationDuration * 0.35f).SetEase(Ease.OutQuad));
            changeSequence.Append(label.rectTransform.DOScale(restingScale, animationDuration * 0.4f).SetEase(Ease.OutBack));
            changeSequence.OnComplete(() => changeSequence = null);
        }

        public void Clear()
        {
            changeSequence?.Kill();
            changeSequence = null;
            label.rectTransform.localScale = restingScale;
            label.text = string.Empty;
            hasDisplayedValue = false;
        }

        private void OnDisable()
        {
            changeSequence?.Kill();
            changeSequence = null;
            label.rectTransform.localScale = restingScale;
            hasDisplayedValue = false;
        }
    }
}
