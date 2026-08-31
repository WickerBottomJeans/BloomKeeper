using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIStartupScreen startupScreenPrefab;
        private UIStartupScreen startupScreenInstance;

        public event Action AuthPlayRequested;

        public void ShowStartupScreen(StartupScreenState state)
        {
            GetPanel(ref startupScreenInstance, startupScreenPrefab, uiRoot);
            UnbindStartupScreen();
            BindStartupScreen();
            startupScreenInstance.Show(state);
            startupScreenInstance.gameObject.SetActive(true);
        }

        public void HideStartupScreen()
        {
            UnbindStartupScreen();
            startupScreenInstance?.gameObject.SetActive(false);
        }

        private void BindStartupScreen()
        {
            startupScreenInstance.PlayRequested += HandleAuthPlayRequested;
        }

        private void UnbindStartupScreen()
        {
            if (startupScreenInstance == null) return;

            startupScreenInstance.PlayRequested -= HandleAuthPlayRequested;
        }

        private void HandleAuthPlayRequested()
        {
            AuthPlayRequested?.Invoke();
        }
    }
}
