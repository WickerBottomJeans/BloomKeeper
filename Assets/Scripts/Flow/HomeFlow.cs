using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        public event Action<int> StartLevelRequested;

        public async UniTask Enter()
        {
            UIManager.Instance.ShowLevelSelect(PlayerAccountContext.Instance.GetCurrentProgression());
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
