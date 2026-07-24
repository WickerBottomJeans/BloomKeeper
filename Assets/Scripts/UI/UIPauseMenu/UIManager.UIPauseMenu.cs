using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIPauseMenu pauseMenuPrefab;
        private UIPauseMenu pauseMenuInstance;

        public event Action PauseMenuResumeRequested;
        public event Action PauseMenuSettingsRequested;
        public event Action PauseMenuQuitRequested;

        public void ShowPauseMenu()
        {
            if (pauseMenuInstance == null)
                pauseMenuInstance = Instantiate(pauseMenuPrefab, uiRoot);

            UnbindPauseMenu();
            BindPauseMenu();
            pauseMenuInstance.Show();
        }

        public void HidePauseMenu()
        {
            UnbindPauseMenu();
            pauseMenuInstance?.Hide();
        }

        private void BindPauseMenu()
        {
            pauseMenuInstance.ResumeRequested += HandlePauseMenuResumeRequested;
            pauseMenuInstance.SettingsRequested += HandlePauseMenuSettingsRequested;
            pauseMenuInstance.QuitRequested += HandlePauseMenuQuitRequested;
        }

        private void UnbindPauseMenu()
        {
            if (pauseMenuInstance == null) return;

            pauseMenuInstance.ResumeRequested -= HandlePauseMenuResumeRequested;
            pauseMenuInstance.SettingsRequested -= HandlePauseMenuSettingsRequested;
            pauseMenuInstance.QuitRequested -= HandlePauseMenuQuitRequested;
        }

        private void HandlePauseMenuResumeRequested()
        {
            PauseMenuResumeRequested?.Invoke();
        }

        private void HandlePauseMenuSettingsRequested()
        {
            PauseMenuSettingsRequested?.Invoke();
        }

        private void HandlePauseMenuQuitRequested()
        {
            PauseMenuQuitRequested?.Invoke();
        }
    }
}
