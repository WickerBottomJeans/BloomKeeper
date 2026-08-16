using System;
using System.Collections.Generic;
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

        public void ShowWinScreen(int stars, int starCap, bool showNext, RewardDisplayData rewardDisplayData)
        {
            GetPanel(ref winScreenInstance, winScreenPrefab, uiRoot);
            UnbindWinScreen();
            BindWinScreen();
            winScreenInstance.Display(stars, starCap, showNext, rewardDisplayData);
            winScreenInstance.Show();
        }

        public void HideWinScreen()
        {
            UnbindWinScreen();
            winScreenInstance?.Hide();
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
