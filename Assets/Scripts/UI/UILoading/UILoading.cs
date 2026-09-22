using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private RawImage screenshotImage;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button spriteChangeButton;
        [SerializeField] private List<Sprite> sprites;
        [SerializeField, Range(0f, 1f)] private float collapsedScale = 0f;
        [SerializeField] private float animationPhaseDuration = 0.08f;
        [SerializeField] private float secondsPerRotation = 5f;
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField, Range(0f, 1f)] private float backgroundDarkening = 0.3f;

        private int currentSpriteIndex;
        private Vector3 baseImageScale;
        private Vector3 baseImageEulerAngles;
        private Sequence spriteChangeSequence;
        private Tween rotationTween;
        private Texture2D screenshotTexture;
        private float loadingBackgroundVisibility;
        private float loadingBackgroundTargetVisibility;
        private Tween loadingBackgroundTween;
        private UniTaskCompletionSource loadingBackgroundTransition;

        private void Awake()
        {
            SetLoadingBackgroundVisibility(0f);
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

        public void CaptureLoadingBackground()
        {
            ReleaseLoadingBackground();
            screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false, false);
            try
            {
                screenshotTexture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                screenshotTexture.Apply();
                screenshotImage.texture = screenshotTexture;
            }
            catch
            {
                ReleaseLoadingBackground();
                throw;
            }
        }

        public void ReleaseLoadingBackground()
        {
            if (screenshotTexture == null) return;
            screenshotImage.texture = null;
            Destroy(screenshotTexture);
            screenshotTexture = null;
        }

        public UniTask ShowLoadingBackground()
        {
            return AnimateLoadingBackground(1f);
        }

        public UniTask HideLoadingBackground()
        {
            return AnimateLoadingBackground(0f);
        }

        private UniTask AnimateLoadingBackground(float targetVisibility)
        {
            if (loadingBackgroundTransition != null && loadingBackgroundTargetVisibility == targetVisibility)
                return loadingBackgroundTransition.Task;

            // Finish the superseded transition.
            loadingBackgroundTransition?.TrySetResult();
            loadingBackgroundTween?.Kill();

            loadingBackgroundTargetVisibility = targetVisibility;
            var transitionCompletionSource = new UniTaskCompletionSource();
            loadingBackgroundTransition = transitionCompletionSource;
            loadingBackgroundTween = DOTween.To(() => loadingBackgroundVisibility, SetLoadingBackgroundVisibility, targetVisibility, fadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => transitionCompletionSource.TrySetResult())
                .OnKill(() => transitionCompletionSource.TrySetCanceled());
            return transitionCompletionSource.Task;
        }

        private void SetLoadingBackgroundVisibility(float visibility)
        {
            loadingBackgroundVisibility = visibility;
            float brightness = 1f - backgroundDarkening;
            screenshotImage.color = new Color(brightness, brightness, brightness, visibility);
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
            ReleaseLoadingBackground();
            loadingBackgroundTween?.Kill();
            loadingBackgroundTween = null;
            loadingBackgroundTransition = null;
            SetLoadingBackgroundVisibility(0f);
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
            ReleaseLoadingBackground();
            loadingBackgroundTween?.Kill();
            spriteChangeButton.onClick.RemoveListener(HandleSpriteChangeClicked);
            spriteChangeSequence?.Kill();
            rotationTween?.Kill();
        }
    }
}
