using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIPauseMenu : UIPopup
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        public event Action ResumeRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(HandleResumeClicked);
            settingsButton.onClick.AddListener(HandleSettingsClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
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
