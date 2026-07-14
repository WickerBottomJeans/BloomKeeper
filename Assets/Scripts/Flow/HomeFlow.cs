using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        public event Action<int> StartLevelRequested;

        public async UniTask Enter(PlayerProgressionData progression)
        {
            UIManager.Instance.ShowLevelSelect(progression);
            UIManager.Instance.LevelSelected += HandleLevelSelected;
            await UIManager.Instance.WaitForLevelSelectInitialBackgroundLoaded();
        }

        public void Exit()
        {
            UIManager.Instance.LevelSelected -= HandleLevelSelected;
            UIManager.Instance.HideLevelSelect();
        }

        private void HandleLevelSelected(int levelId)
        {
            StartLevelRequested?.Invoke(levelId);
        }
    }
}
