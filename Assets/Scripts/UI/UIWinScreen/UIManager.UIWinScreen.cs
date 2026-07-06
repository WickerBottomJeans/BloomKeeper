using System.Collections.Generic;
using DefaultNamespace;
using UI;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIWinScreen winScreenPrefab;
        private UIWinScreen winScreenInstance;

        public UIWinScreen ShowWinScreen()
        {
            if (winScreenInstance != null)
            {
                Destroy(winScreenInstance.gameObject);
            }

            winScreenInstance = Instantiate(winScreenPrefab, uiRoot);
            return winScreenInstance;
        }

        public void HideWinScreen()
        {
            winScreenInstance?.gameObject.SetActive(false);
        }
    }
}
