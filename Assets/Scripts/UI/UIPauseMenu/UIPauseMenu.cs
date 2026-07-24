using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UIPauseMenu : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        public event Action ResumeRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            resumeButton.onClick.AddListener(HandleResumeClicked);
            settingsButton.onClick.AddListener(HandleSettingsClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleResumeClicked()
        {
            ResumeRequested?.Invoke();
        }

        private void HandleSettingsClicked()
        {
            SettingsRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            QuitRequested?.Invoke();
        }
    }
}
