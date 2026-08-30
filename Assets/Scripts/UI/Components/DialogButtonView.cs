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
        [SerializeField] private DialogButtonColorVariant colorVariant = DialogButtonColorVariant.Green;
        [SerializeField] private UIButtonPressFeedback pressFeedback;

        public event Action Clicked;

        private void Awake()
        {
            button.onClick.AddListener(HandleClicked);
            targetImage.sprite = styleConfig.GetStyle(colorVariant).sprite;
        }

        public void Configure(string text, DialogButtonColorVariant colorVariant)
        {
            label.text = text;
            this.colorVariant = colorVariant;
            targetImage.sprite = styleConfig.GetStyle(colorVariant).sprite;
            button.interactable = true;
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
