using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        private UILevelSelect levelSelect;

        public event Action<int> StartLevelRequested;

        public async UniTask Enter()
        {
            levelSelect = UIManager.Instance.ShowLevelSelect();
            levelSelect.OnLevelSelected += HandleLevelSelected;
            await UIManager.Instance.WaitForLevelSelectInitialBackgroundLoaded();
        }

        public void Exit()
        {
            if (levelSelect != null)
                levelSelect.OnLevelSelected -= HandleLevelSelected;
            UIManager.Instance.HideLevelSelect();
        }

        private void HandleLevelSelected(int levelId)
        {
            StartLevelRequested?.Invoke(levelId);
        }
    }
}
