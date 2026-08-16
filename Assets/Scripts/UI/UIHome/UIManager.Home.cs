using System;
using System.Collections.Generic;
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
        public event Action<int> ChapterVisitRequested;
        public event Action ChapterChooserCloseRequested;

        public async UniTask ShowHome(ChapterContent chapter, PlayerProgressionData progression, PlayerLivesViewData lives)
        {
            GetPanel(ref homeInstance, homePrefab, uiRoot);
            UnbindHome();
            BindHome();
            await homeInstance.ShowAsync(chapter, progression, lives);
        }

        public void DisplayHomeLives(PlayerLivesViewData lives)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display Home lives before UIHome has been shown.");
            homeInstance.DisplayLives(lives);
        }

        public async UniTask DisplayHomeTabAsync(HomeMiddleTab tab)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display a Home tab before UIHome has been shown.");
            await homeInstance.DisplayMiddleTabAsync(tab);
        }

        public async UniTask PrepareChapterChooserAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot prepare the chapter chooser before UIHome has been shown.");
            await homeInstance.PrepareChapterChooserAsync(chapterStates);
        }

        public void ShowChapterChooser()
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot show the chapter chooser before UIHome has been shown.");
            homeInstance.ShowChapterChooser();
        }

        public void HideChapterChooser()
        {
            homeInstance?.HideChapterChooser();
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
            homeInstance.ChapterVisitRequested += HandleChapterVisitRequested;
            homeInstance.ChapterChooserCloseRequested += HandleChapterChooserCloseRequested;
        }

        private void UnbindHome()
        {
            if (homeInstance == null) return;

            homeInstance.LevelSelected -= HandleHomeLevelSelected;
            homeInstance.TabRequested -= HandleHomeTabRequested;
            homeInstance.SettingsRequested -= HandleHomeSettingsRequested;
            homeInstance.AddLifeRequested -= HandleHomeAddLifeRequested;
            homeInstance.AddCurrencyRequested -= HandleHomeAddCurrencyRequested;
            homeInstance.ChapterVisitRequested -= HandleChapterVisitRequested;
            homeInstance.ChapterChooserCloseRequested -= HandleChapterChooserCloseRequested;
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

        private void HandleChapterVisitRequested(int chapterId)
        {
            ChapterVisitRequested?.Invoke(chapterId);
        }

        private void HandleChapterChooserCloseRequested()
        {
            ChapterChooserCloseRequested?.Invoke();
        }
    }
}
