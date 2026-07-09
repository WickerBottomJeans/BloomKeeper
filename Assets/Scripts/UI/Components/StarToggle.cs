using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class StarToggle : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private float toggleAnimationTime = 0.12f;

        private Sequence toggleSequence;
        private bool isOn;
        private bool hasState;

        public float ToggleAnimationTime => toggleAnimationTime;

        public void SetImmediate(bool isOn)
        {
            toggleSequence?.Kill();
            this.isOn = isOn;
            hasState = true;
            transform.localScale = Vector3.one;
            image.sprite = isOn ? onSprite : offSprite;
        }

        public void SetOn(bool isOn, float delay = 0f)
        {
            if (hasState && this.isOn == isOn) return;

            this.isOn = isOn;
            hasState = true;
            toggleSequence?.Kill();
            toggleSequence = DOTween.Sequence();
            if (delay > 0f)
                toggleSequence.AppendInterval(delay);
            toggleSequence.Append(transform.DOScaleX(0f, toggleAnimationTime * 0.5f).SetEase(Ease.InQuad));
            toggleSequence.AppendCallback(() => image.sprite = isOn ? onSprite : offSprite);
            toggleSequence.Append(transform.DOScaleX(1f, toggleAnimationTime * 0.5f).SetEase(Ease.OutQuad));
        }
    }
}
