using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class DialogButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private DialogButtonStyleConfig styleConfig;
        [SerializeField] private DialogButtonVariant variant = DialogButtonVariant.Green;
        [SerializeField] private UIButtonPressFeedback pressFeedback;

        public event Action Clicked;

        private void Awake()
        {
            button.onClick.AddListener(HandleClicked);
            targetImage.sprite = styleConfig.GetStyle(variant).sprite;
        }

        public void Configure(string text, DialogButtonVariant variant, bool interactable = true)
        {
            label.text = text;
            this.variant = variant;
            targetImage.sprite = styleConfig.GetStyle(variant).sprite;
            button.interactable = interactable;
            if (!interactable) pressFeedback.ResetImmediately();
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            if (!interactable) pressFeedback.ResetImmediately();
        }

        private void HandleClicked()
        {
            Clicked?.Invoke();
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }
}
