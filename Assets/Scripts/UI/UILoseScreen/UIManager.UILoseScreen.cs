using UI;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILoseScreen loseScreenPrefab;
        private UILoseScreen loseScreenInstance;

        public UILoseScreen ShowLoseScreen(string message)
        {
            if (loseScreenInstance == null)
                loseScreenInstance = Instantiate(loseScreenPrefab, uiRoot);
            loseScreenInstance.gameObject.SetActive(true);
            loseScreenInstance.Display(message);
            return loseScreenInstance;
        }

        public void HideLoseScreen()
        {
            loseScreenInstance?.gameObject.SetActive(false);
        }
    }
}
