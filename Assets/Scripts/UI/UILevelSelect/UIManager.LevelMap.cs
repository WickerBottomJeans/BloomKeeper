using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILevelSelect levelSelectPrefab;
        private UILevelSelect levelSelectInstance;

        public event Action<int> LevelSelected;

        public void ShowLevelSelect(PlayerProgressionData progression)
        {
            if (levelSelectInstance == null)
                levelSelectInstance = Instantiate(levelSelectPrefab, uiRoot);
            UnbindLevelSelect();
            BindLevelSelect();
            levelSelectInstance.Show(progression);
        }

        public UniTask WaitForLevelSelectInitialBackgroundLoaded()
        {
            return levelSelectInstance.WaitForInitialBackgroundLoaded();
        }

        public void HideLevelSelect()
        {
            UnbindLevelSelect();
            levelSelectInstance?.Hide();
        }

        private void BindLevelSelect()
        {
            levelSelectInstance.OnLevelSelected += HandleLevelSelected;
        }

        private void UnbindLevelSelect()
        {
            if (levelSelectInstance == null) return;

            levelSelectInstance.OnLevelSelected -= HandleLevelSelected;
        }

        private void HandleLevelSelected(int levelId)
        {
            LevelSelected?.Invoke(levelId);
        }

    }
}
