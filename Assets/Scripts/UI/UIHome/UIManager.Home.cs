using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIHome homePrefab;
        private UIHome homeInstance;

        public event Action<int> LevelSelected;
        public event Action<HomeMiddleTab> HomeTabRequested;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        public async UniTask ShowHome(ChapterContent chapter, PlayerProgressionData progression)
        {
            if (homeInstance == null)
                homeInstance = Instantiate(homePrefab, uiRoot);

            UnbindHome();
            BindHome();
            await homeInstance.ShowAsync(chapter, progression);
        }

        public async UniTask DisplayHomeTabAsync(HomeMiddleTab tab)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display a Home tab before UIHome has been shown.");
            await homeInstance.DisplayMiddleTabAsync(tab);
        }

        public void HideHome()
        {
            UnbindHome();
            homeInstance?.Hide();
        }

        private void BindHome()
        {
            homeInstance.LevelSelected += HandleHomeLevelSelected;
            homeInstance.TabRequested += HandleHomeTabRequested;
            homeInstance.SettingsRequested += HandleHomeSettingsRequested;
            homeInstance.AddLifeRequested += HandleHomeAddLifeRequested;
            homeInstance.AddCurrencyRequested += HandleHomeAddCurrencyRequested;
        }

        private void UnbindHome()
        {
            if (homeInstance == null) return;

            homeInstance.LevelSelected -= HandleHomeLevelSelected;
            homeInstance.TabRequested -= HandleHomeTabRequested;
            homeInstance.SettingsRequested -= HandleHomeSettingsRequested;
            homeInstance.AddLifeRequested -= HandleHomeAddLifeRequested;
            homeInstance.AddCurrencyRequested -= HandleHomeAddCurrencyRequested;
        }

        private void HandleHomeLevelSelected(int levelId)
        {
            LevelSelected?.Invoke(levelId);
        }

        private void HandleHomeTabRequested(HomeMiddleTab tab)
        {
            HomeTabRequested?.Invoke(tab);
        }

        private void HandleHomeSettingsRequested()
        {
            SettingsRequested?.Invoke();
        }

        private void HandleHomeAddLifeRequested()
        {
            AddLifeRequested?.Invoke();
        }

        private void HandleHomeAddCurrencyRequested()
        {
            AddCurrencyRequested?.Invoke();
        }
    }
}
