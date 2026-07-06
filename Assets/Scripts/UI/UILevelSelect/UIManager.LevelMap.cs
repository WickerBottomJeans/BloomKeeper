using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILevelSelect levelSelectPrefab;
        private UILevelSelect levelSelectInstance;

        public UILevelSelect ShowLevelSelect()
        {
            if (levelSelectInstance == null)
                levelSelectInstance = Instantiate(levelSelectPrefab, uiRoot);
            levelSelectInstance.Show();
            return levelSelectInstance;
        }

        public UniTask WaitForLevelSelectInitialBackgroundLoaded()
        {
            return levelSelectInstance.WaitForInitialBackgroundLoaded();
        }

        public void HideLevelSelect()
        {
            levelSelectInstance?.Hide();
        }
    }
}
