using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIDialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private DialogButtonView buttonPrefab;
        [SerializeField] private RectTransform buttonRoot;

        private readonly List<DialogButtonView> activeButtons = new();
        
        public event Action<int> ButtonClicked;

        private void Awake()
        {
            buttonPrefab.gameObject.SetActive(false);
        }

        public void Display(string title, string message, IReadOnlyList<DialogOptionButton> options)
        {
            titleText.text = title;
            messageText.text = message;
            RefreshButtons(options);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
            int buttonId = optionButton.Id;
            button.Configure(optionButton.Label, optionButton.Variant);
            button.Clicked += () => HandleButtonClicked(buttonId);
            activeButtons.Add(button);
            button.gameObject.SetActive(true);
        }

        private void HandleButtonClicked(int buttonId)
        {
            ButtonClicked?.Invoke(buttonId);
        }
    }
}
