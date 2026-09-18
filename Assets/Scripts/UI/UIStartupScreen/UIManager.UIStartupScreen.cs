using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIStartupScreen startupScreenPrefab;
        private UIStartupScreen startupScreenInstance;

        public event Action AuthPlayRequested;

        public void ShowStartupScreen()
        {
            GetPanel(ref startupScreenInstance, startupScreenPrefab, uiRoot);
            UnbindStartupScreen();
            BindStartupScreen();
            startupScreenInstance.gameObject.SetActive(true);
        }

        public void SetStartupAccountEntryVisible(bool visible)
        {
            startupScreenInstance.SetAccountEntryVisible(visible);
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
