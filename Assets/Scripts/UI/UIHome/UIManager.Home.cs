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
        /// <summary>
        /// Shop offer the player asked to buy.
        /// </summary>
        public event Action<string> HomeShopOfferBuyRequested;

        public async UniTask ShowHome(string topperPrefabAddress, string bottomNavigationPrefabAddress, PlayerLivesViewData lives, int diamondQuantity)
        {
            GetPanel(ref homeInstance, homePrefab, uiRoot);
            UnbindHome();
            BindHome();
            await homeInstance.ShowAsync(topperPrefabAddress, bottomNavigationPrefabAddress, lives, diamondQuantity);
        }

        public void DisplayHomeLives(PlayerLivesViewData lives)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display Home lives before UIHome has been shown.");
            homeInstance.DisplayLives(lives);
        }

        public void DisplayHomeCurrency(int diamondQuantity)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display Home currency before UIHome has been shown.");
            homeInstance.DisplayCurrency(diamondQuantity);
        }

        public async UniTask DisplayHomeMapAsync(ChapterContent chapter, PlayerProgressionData progression)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display the Home Map before UIHome has been shown.");
            await homeInstance.DisplayMapAsync(chapter, progression);
        }

        /// <summary>
        /// Displays shop data in the Home panel.
        /// </summary>
        public void DisplayHomeShop(IReadOnlyList<ShopOfferDisplayData> shopOfferDisplayDataList)
        {
            if (homeInstance == null) throw new InvalidOperationException("Cannot display the Home Shop before UIHome has been shown.");
            homeInstance.DisplayShop(shopOfferDisplayDataList);
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
            homeInstance.ShopOfferBuyRequested += HandleHomeShopOfferBuyRequested;
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
            homeInstance.ShopOfferBuyRequested -= HandleHomeShopOfferBuyRequested;
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

        /// <summary>
        /// Raises HomeShopOfferBuyRequested with the selected offer ID.
        /// </summary>
        private void HandleHomeShopOfferBuyRequested(string offerId)
        {
            HomeShopOfferBuyRequested?.Invoke(offerId);
        }
    }
}
