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
                levelSelectInstance = Instantiate(levelSelectPrefab, canvas.transform);
            levelSelectInstance.Show();
            return levelSelectInstance;
        }

        public void HideLevelSelect()
        {
            levelSelectInstance?.Hide();
        }
    }
}
