using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UILoading : MonoBehaviour
    {
        private const string DefaultText = "Loading . . .";

        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button spriteChangeButton;
        [SerializeField] private List<Sprite> sprites;
        [SerializeField, Range(0f, 1f)] private float collapsedScale = 0f;
        [SerializeField] private float animationPhaseDuration = 0.08f;
        [SerializeField] private float secondsPerRotation = 5f;

        private int currentSpriteIndex;
        private Vector3 baseImageScale;
        private Vector3 baseImageEulerAngles;
        private Sequence spriteChangeSequence;
        private Tween rotationTween;

        private void Awake()
        {
            baseImageScale = image.rectTransform.localScale;
            baseImageEulerAngles = image.rectTransform.localEulerAngles;
            image.sprite = sprites[currentSpriteIndex];
            spriteChangeButton.onClick.AddListener(HandleSpriteChangeClicked);
        }

        private void OnEnable()
        {
            SetRandomSprite();
            image.rectTransform.localEulerAngles = baseImageEulerAngles;
            rotationTween?.Kill();
            rotationTween = image.rectTransform.DOLocalRotate(baseImageEulerAngles + new Vector3(0f, 0f, -360f), secondsPerRotation, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void SetText(string text = DefaultText)
        {
            label.text = text;
        }

        private void HandleSpriteChangeClicked()
        {
            spriteChangeButton.interactable = false;
            spriteChangeSequence?.Kill();
            spriteChangeSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(image.rectTransform.DOScale(baseImageScale * collapsedScale, animationPhaseDuration).SetEase(Ease.InQuad))
                .AppendCallback(AdvanceSprite)
                .Append(image.rectTransform.DOScale(baseImageScale, animationPhaseDuration).SetEase(Ease.OutBack))
                .OnComplete(CompleteSpriteChange);
        }

        private void AdvanceSprite()
        {
            currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Count;
            image.sprite = sprites[currentSpriteIndex];
        }

        private void SetRandomSprite()
        {
            currentSpriteIndex = Random.Range(0, sprites.Count);
            image.sprite = sprites[currentSpriteIndex];
        }

        private void CompleteSpriteChange()
        {
            spriteChangeButton.interactable = true;
            spriteChangeSequence = null;
        }

        private void OnDisable()
        {
            spriteChangeSequence?.Kill();
            spriteChangeSequence = null;
            rotationTween?.Kill();
            rotationTween = null;
            image.rectTransform.localScale = baseImageScale;
            image.rectTransform.localEulerAngles = baseImageEulerAngles;
            spriteChangeButton.interactable = true;
        }

        private void OnDestroy()
        {
            spriteChangeButton.onClick.RemoveListener(HandleSpriteChangeClicked);
            spriteChangeSequence?.Kill();
            rotationTween?.Kill();
        }
    }
}
