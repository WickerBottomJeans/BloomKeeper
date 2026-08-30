using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIDialog : UIPopup
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private DialogButtonView buttonPrefab;
        [SerializeField] private RectTransform buttonRoot;

        private readonly List<DialogButtonView> activeButtons = new();
        
        public event Action<DialogButtonType> ButtonClicked;

        protected override void Awake()
        {
            base.Awake();
            buttonPrefab.gameObject.SetActive(false);
        }

        public void Display(string title, string message, IReadOnlyList<DialogOptionButton> options)
        {
            titleText.text = title;
            messageText.text = message;
            RefreshButtons(options);
        }

        public void SetButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < activeButtons.Count; i++)
                activeButtons[i].SetInteractable(interactable);
        }

        private void RefreshButtons(IReadOnlyList<DialogOptionButton> options)
        {
            ClearButtons();

            for (int i = 0; i < options.Count; i++)
                SpawnButton(options[i]);
        }

        private void ClearButtons()
        {
            activeButtons.Clear();

            for (int i = buttonRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = buttonRoot.GetChild(i);
                if (child == buttonPrefab.transform) continue;
                Destroy(child.gameObject);
            }
        }

        private void SpawnButton(DialogOptionButton optionButton)
        {
            DialogButtonView button = Instantiate(buttonPrefab, buttonRoot);
            DialogButtonType buttonType = optionButton.ButtonType;
            button.Configure(optionButton.Label, optionButton.ColorVariant);
            button.Clicked += () => HandleButtonClicked(buttonType);
            activeButtons.Add(button);
            button.gameObject.SetActive(true);
        }

        private void HandleButtonClicked(DialogButtonType buttonType)
        {
            ButtonClicked?.Invoke(buttonType);
        }
    }
}
