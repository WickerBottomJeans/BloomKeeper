using System;
using UI;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILoseScreen loseScreenPrefab;
        private UILoseScreen loseScreenInstance;

        public event Action LoseScreenRetryRequested;
        public event Action LoseScreenHomeRequested;

        public void ShowLoseScreen(string message)
        {
            if (loseScreenInstance == null)
                loseScreenInstance = Instantiate(loseScreenPrefab, uiRoot);

            UnbindLoseScreen();
            BindLoseScreen();
            loseScreenInstance.gameObject.SetActive(true);
            loseScreenInstance.Display(message);
        }

        public void HideLoseScreen()
        {
            UnbindLoseScreen();
            loseScreenInstance?.gameObject.SetActive(false);
        }

        private void BindLoseScreen()
        {
            loseScreenInstance.RetryRequested += HandleLoseScreenRetryRequested;
            loseScreenInstance.HomeRequested += HandleLoseScreenHomeRequested;
        }

        private void UnbindLoseScreen()
        {
            if (loseScreenInstance == null) return;

            loseScreenInstance.RetryRequested -= HandleLoseScreenRetryRequested;
            loseScreenInstance.HomeRequested -= HandleLoseScreenHomeRequested;
        }

        private void HandleLoseScreenRetryRequested()
        {
            LoseScreenRetryRequested?.Invoke();
        }

        private void HandleLoseScreenHomeRequested()
        {
            LoseScreenHomeRequested?.Invoke();
        }
    }
}
