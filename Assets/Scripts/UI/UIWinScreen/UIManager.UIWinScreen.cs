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

        public void ShowWinScreen(Texture levelResultBackgroundTexture, int stars, int starCap, bool showNext, IReadOnlyList<string> completionRewardPresentationKeys)
        {
            if (winScreenInstance == null)
                winScreenInstance = Instantiate(winScreenPrefab, uiRoot);

            UnbindWinScreen();
            BindWinScreen();
            winScreenInstance.Display(levelResultBackgroundTexture, stars, starCap, showNext, completionRewardPresentationKeys);
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
