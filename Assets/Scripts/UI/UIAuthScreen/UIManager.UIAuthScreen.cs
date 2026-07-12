using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIAuthScreen authScreenPrefab;
        private UIAuthScreen authScreenInstance;

        public event Action AuthPlayRequested;

        public void ShowAuthScreen(string playButtonText)
        {
            if (authScreenInstance == null)
                authScreenInstance = Instantiate(authScreenPrefab, uiRoot);

            UnbindAuthScreen();
            BindAuthScreen();
            authScreenInstance.Display(playButtonText);
            authScreenInstance.gameObject.SetActive(true);
        }

        public void HideAuthScreen()
        {
            UnbindAuthScreen();
            authScreenInstance?.gameObject.SetActive(false);
        }

        private void BindAuthScreen()
        {
            authScreenInstance.PlayRequested += HandleAuthPlayRequested;
        }

        private void UnbindAuthScreen()
        {
            if (authScreenInstance == null) return;

            authScreenInstance.PlayRequested -= HandleAuthPlayRequested;
        }

        private void HandleAuthPlayRequested()
        {
            AuthPlayRequested?.Invoke();
        }
    }
}
