using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        private UILevelSelect levelSelect;

        public event Action<int> StartLevelRequested;

        public void Enter()
        {
            levelSelect = UIManager.Instance.ShowLevelSelect();
            levelSelect.OnLevelSelected += HandleLevelSelected;
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
