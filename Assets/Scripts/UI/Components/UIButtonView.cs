using System;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image targetImage;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private UIButtonStyleConfig styleConfig;
        [SerializeField] private UIButtonVariant variant = UIButtonVariant.Green;
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float scaleDuration = 0.2f;

        private Vector3 baseScale;
        private bool isPressed;

        public event Action Clicked;

        private void Awake()
        {
            baseScale = visualRoot.localScale;
            button.onClick.AddListener(HandleClicked);
            targetImage.sprite = styleConfig.GetStyle(variant).sprite;
        }

        public void Configure(string text, UIButtonVariant variant, bool interactable = true)
        {
            label.text = text;
            this.variant = variant;
            targetImage.sprite = styleConfig.GetStyle(variant).sprite;
            button.interactable = interactable;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            if (!interactable)
                PlayReleaseAnimation();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;

            isPressed = true;
            visualRoot.DOKill();
            visualRoot.DOScale(baseScale * pressedScale, scaleDuration).SetEase(Ease.OutQuad).SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
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

        private void PlayReleaseAnimation()
        {
            isPressed = false;
            visualRoot.DOKill();
            visualRoot.DOScale(baseScale, scaleDuration).SetEase(Ease.OutQuad).SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void HandleClicked()
        {
            Clicked?.Invoke();
        }

        private void OnDisable()
        {
            isPressed = false;
            visualRoot.DOKill();
            visualRoot.localScale = baseScale;
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClicked);
            visualRoot.DOKill();
        }
    }
}
