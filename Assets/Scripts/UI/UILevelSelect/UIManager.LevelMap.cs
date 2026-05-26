using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILevelSelect levelSelectPrefab;
        private UILevelSelect levelSelectInstance;

        public void ShowLevelSelect()
        {
            if (levelSelectInstance == null)
                levelSelectInstance = Instantiate(levelSelectPrefab, canvas.transform);
            levelSelectInstance.Show();
        }

        public void HideLevelSelect()
        {
            levelSelectInstance?.Hide();
        }
    }
}