using System;
using UI;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIWinScreen winScreenPrefab;
        private UIWinScreen winScreenInstance;

        public event Action WinScreenHomeRequested;
        public event Action WinScreenNextRequested;

        public void ShowWinScreen(int stars, int starCap)
        {
            if (winScreenInstance == null)
                winScreenInstance = Instantiate(winScreenPrefab, uiRoot);

            UnbindWinScreen();
            BindWinScreen();
            winScreenInstance.DisplayStars(stars, starCap);
            winScreenInstance.gameObject.SetActive(true);
        }

        public void HideWinScreen()
        {
            UnbindWinScreen();
            winScreenInstance?.gameObject.SetActive(false);
        }

        private void BindWinScreen()
        {
            winScreenInstance.HomeRequested += HandleWinScreenHomeRequested;
            winScreenInstance.NextRequested += HandleWinScreenNextRequested;
        }

        private void UnbindWinScreen()
        {
            if (winScreenInstance == null) return;

            winScreenInstance.HomeRequested -= HandleWinScreenHomeRequested;
            winScreenInstance.NextRequested -= HandleWinScreenNextRequested;
        }

        private void HandleWinScreenHomeRequested()
        {
            WinScreenHomeRequested?.Invoke();
        }

        private void HandleWinScreenNextRequested()
        {
            WinScreenNextRequested?.Invoke();
        }
    }
}
