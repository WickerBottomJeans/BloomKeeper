using UI;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILoseScreen loseScreenPrefab;
        private UILoseScreen loseScreenInstance;

        public void ShowLoseScreen(string message)
        {
            if (loseScreenInstance == null)
                loseScreenInstance = Instantiate(loseScreenPrefab, canvas.transform);
            loseScreenInstance.gameObject.SetActive(true);
            loseScreenInstance.Display(message);
        }

        public void HideLoseScreen()
        {
            loseScreenInstance?.gameObject.SetActive(false);
        }
    }
}
